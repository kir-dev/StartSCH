using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using HtmlAgilityPack;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.SchPincer;

public class SchPincerPollJob(
    Db db,
    IMemoryCache cache,
    NotificationService notificationService,
    NotificationQueueService notificationQueueService,
    HttpClient httpClient,
    ILogger<SchPincerPollJob> logger)
    : IPollJobExecutor
{
    private readonly DateTime _utcNow = DateTime.UtcNow;

    public async Task Execute(CancellationToken cancellationToken)
    {
        // The home page returns all openings *ending* less than a week from now.
        // Includes group name, opening title and opening start.
        // https://github.com/kir-dev/sch-pincer/blob/33ee7f/src/main/kotlin/hu/kirdev/schpincer/web/MainController.kt#L40
        // https://github.com/kir-dev/sch-pincer/blob/33ee7f/src/main/kotlin/hu/kirdev/schpincer/service/OpeningService.kt#L42
        // https://github.com/kir-dev/sch-pincer/blob/33ee7f/src/main/resources/templates/index.html#L106

        // /api/items returns the start of up to 1 opening per group and only if there is a public item for that
        // opening.
        // Includes group ID, group name, opening start, orderability and out-of-stock status and a few other things,
        // but not the opening title.
        // https://github.com/kir-dev/sch-pincer/blob/33ee7f/src/main/kotlin/hu/kirdev/schpincer/web/ApiController.kt#L83
        // https://github.com/kir-dev/sch-pincer/blob/33ee7f/src/main/kotlin/hu/kirdev/schpincer/dto/ItemEntityDto.kt#L33

        // request concurrently, hopefully minimizing inconsistencies
        var homepageTask = httpClient.GetStreamAsync(
            "https://schpincer.sch.bme.hu", cancellationToken);
        var itemsTask = httpClient.GetFromJsonAsync<List<PincerItem>>(
            "https://schpincer.sch.bme.hu/api/items", cancellationToken);

        List<Page> pages;

        List<PincerItem> pincerItems = await itemsTask ?? throw new("Null returned by Pincer /api/items");

        // prevent things like a group being created simultaneously from both PeK and Pincer
        await using (var transaction =
                     await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
        {
            pages = await db.Pages
                .Include(p => p.Categories)
                .ToListAsync(cancellationToken);
            
            UpdatePages(pages, pincerItems);

            int rowsAffected = await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            
            if (rowsAffected > 0)
                cache.Remove(CategoryService.CacheKey);
        }

        // scrape upcoming openings from home page
        //
        // Pincer uses a query roughly like this:
        // SELECT circle.displayName, circle.alias, opening.dateStart, opening.feeling, circle.id
        // WHERE opening.dateEnd > now AND opening.dateEnd < now + week
        // ORDER BY opening.dateStart
        HtmlDocument doc = new();
        doc.Load(await homepageTask);
        List<OpeningOverview> previews = doc.DocumentNode.ChildNodes
            .Descendants("table")
            .First()
            .ChildNodes
            .Where(n => n.Name == "tr")
            .Select(tr =>
                {
                    var tds = tr.ChildNodes.Where(n => n.Name == "td").ToArray();
                    DateTime startHu = DateTime.ParseExact(
                        tds.First(n => n.HasClass("date")).InnerText,
                        "HH:mm (yy-MM-dd)",
                        CultureInfo.InvariantCulture
                    );
                    var groupName = tds.First().ChildNodes.FindFirst("a").InnerText;
                    var title = tds.First(n => n.HasClass("feeling")).InnerText;
                    if (string.IsNullOrWhiteSpace(title))
                        title = groupName + " nyitás";
                    return new OpeningOverview(
                        groupName,
                        new(startHu, Utils.HungarianTimeZone.GetUtcOffset(startHu)),
                        title
                    );
                }
            )
            .ToList();

        await db.Openings
            .Include(o => o.Categories)
            .ThenInclude(c => c.Page)
            .Where(o => !o.EndUtc.HasValue)
            .LoadAsync(cancellationToken);

        var pincerGroups = pages.Where(g => g.PincerName != null);
        var pincerNameToOverviews = previews.GroupBy(p => p.GroupName).ToDictionary(p => p.Key);
        var pincerNameToItems = pincerItems.GroupBy(i => i.CircleName).ToDictionary(i => i.Key);
        HashSet<Opening> orderingStarted = [];
        foreach (Page group in pincerGroups)
        {
            UpdateOpenings(
                group,
                pincerNameToOverviews.GetValueOrDefault(group.PincerName!) ?? Enumerable.Empty<OpeningOverview>(),
                pincerNameToItems.GetValueOrDefault(group.PincerName!)?.ToList() ?? [],
                orderingStarted);
        }

        if (orderingStarted.Count > 3) // fail-safe
            orderingStarted.Clear();
        foreach (Opening opening in orderingStarted)
            await notificationService.CreateOrderingStartedNotification(opening);

        await db.SaveChangesAsync(cancellationToken);
        cache.Remove(SchPincerModule.PincerGroupsCacheKey);
        if (orderingStarted.Count != 0) notificationQueueService.Notify();
    }

    private void UpdatePages(
        List<Page> pages,
        IEnumerable<PincerItem> pincerItems)
    {
        var itemsByGroup = pincerItems.GroupBy(i => i.CircleId).ToList();

        HashSet<PincerItem> incomingGroups = itemsByGroup.Select(g => g.First()).ToHashSet();
        HashSet<Page> unmatchedPages = new(pages);

        // already saved, update name
        Dictionary<int, Page> pincerIdToLocalGroup = unmatchedPages
            .Where(g => g.PincerId.HasValue)
            .ToDictionary(g => g.PincerId!.Value);
        foreach (var group in incomingGroups.ToList())
        {
            if (!pincerIdToLocalGroup.TryGetValue(group.CircleId, out var localGroup))
                continue;
            localGroup.PincerName = group.CircleName;
            incomingGroups.Remove(group);
            unmatchedPages.Remove(localGroup);
        }

        // never seen from Pincer, might have been seen from PeK

        // check same PeK name
        Dictionary<string, Page> pekNameToLocalGroup = unmatchedPages
            .Where(g => g.PekName != null)
            .ToDictionary(g => g.PekName!);
        foreach (var group in incomingGroups.ToList())
        {
            if (!pekNameToLocalGroup.TryGetValue(group.CircleName, out var localGroup))
                continue;
            localGroup.PincerId = group.CircleId;
            localGroup.PincerName = group.CircleName;
            incomingGroups.Remove(group);
            unmatchedPages.Remove(localGroup);

            logger.LogInformation(
                "Pincer ID {PincerId} and name {PincerName} added to existing PeK group with ID {PekId} and name {PekName}, found by same name.",
                localGroup.PincerId, localGroup.PincerName, localGroup.PekId, localGroup.PekName);
        }

        // check similar PeK name
        foreach (var group in incomingGroups.ToList())
        {
            // throw if there are multiple matches
            var localGroup = unmatchedPages.SingleOrDefault(g => g.PekName?.RoughlyMatches(group.CircleName) ?? false);
            if (localGroup == null)
                continue;
            localGroup.PincerId = group.CircleId;
            localGroup.PincerName = group.CircleName;
            incomingGroups.Remove(group);
            unmatchedPages.Remove(localGroup);

            logger.LogInformation(
                "Pincer ID {PincerId} and name {PincerName} added to existing PeK group with ID {PekId} and name {PekName}, found by similar name.",
                localGroup.PincerId, localGroup.PincerName, localGroup.PekId, localGroup.PekName);
        }

        // never seen, init from Pincer
        foreach (var incomingGroup in incomingGroups)
        {
            Page page = new()
            {
                PincerId = incomingGroup.CircleId,
                PincerName = incomingGroup.CircleName,
            };
            page.Categories.Add(new() { Page = page });
            db.Pages.Add(page);
            pages.Add(page);

            logger.LogInformation("New group created from Pincer ID {PincerId} and name {PincerName}",
                page.PincerId, page.PincerName);
        }
    }

    // opening update types:
    // - added
    // - canceled
    // - moved
    // - ended
    // assume only one happens per poll
    private void UpdateOpenings(
        Page page,
        IEnumerable<OpeningOverview> overviewsForGroup,
        List<PincerItem> itemsForGroup,
        HashSet<Opening> requiresNotification
    )
    {
        HashSet<OpeningOverview> overviews = new(overviewsForGroup);
        HashSet<Opening> unfinishedOpenings = new(page.Categories.SelectMany(c => c.Events).Cast<Opening>());

        // handle overviews
        while (overviews.Count > 0)
        {
            // take the overview with the best matching opening
            (OpeningOverview overview, Opening? opening) = overviews
                .Select(overview =>
                (
                    Overview: overview,
                    ClosestOpening: unfinishedOpenings.MinBy(opening => GetDistance(opening, overview))
                ))
                .MinBy(p => p.ClosestOpening != null
                    ? GetDistance(p.ClosestOpening, p.Overview)
                    : TimeSpan.MaxValue);

            overviews.Remove(overview);

            if (opening != null) // existing opening, update
            {
                opening.StartUtc = overview.Start.UtcDateTime;
                opening.Title = overview.Title;

                unfinishedOpenings.Remove(opening);
            }
            else // no match, create
            {
                opening = new()
                {
                    Categories = { page.Categories[0] },
                    CreatedUtc = _utcNow,
                    StartUtc = overview.Start.UtcDateTime,
                    Title = overview.Title,
                };
                db.Openings.Add(opening);
            }

            UpdateOrderingInfo(opening, itemsForGroup, requiresNotification);
        }

        // handle openings that didn't get matched to an overview
        foreach (Opening opening in unfinishedOpenings)
        {
            bool hasStarted = opening.StartUtc <= _utcNow;
            if (hasStarted)
            {
                // disappeared because it ended
                opening.EndUtc = _utcNow;

                if (opening is { OrderingStartUtc: not null, OrderingEndUtc: null })
                    opening.OrderingEndUtc = _utcNow;
            }
            else
            {
                // disappeared without starting, probably canceled
                db.Openings.Remove(opening);
            }
        }
    }

    private void UpdateOrderingInfo(Opening opening, List<PincerItem> items, HashSet<Opening> requiresNotification)
    {
        var orderingStarted = opening.OrderingStartUtc.HasValue;
        var orderable = items.Any(i => i.NextOpeningDateUtc == opening.StartUtc && i.Orderable);
        var outOfStock = items.Where(i => i.NextOpeningDateUtc == opening.StartUtc).All(i => i.OutOfStock);

        if (orderable && !outOfStock && !orderingStarted)
        {
            opening.OrderingStartUtc = _utcNow;
            requiresNotification.Add(opening);
        }

        if (orderingStarted && !orderable && !opening.OrderingEndUtc.HasValue)
        {
            opening.OrderingEndUtc = _utcNow;
        }

        if (orderingStarted && outOfStock && !opening.OutOfStockUtc.HasValue)
        {
            opening.OutOfStockUtc = _utcNow;
        }
    }

    record OpeningOverview(string GroupName, DateTimeOffset Start, string Title);

    private static TimeSpan GetDistance(Opening opening, OpeningOverview overview) =>
        (overview.Start.UtcDateTime - opening.StartUtc).Duration();

}

// https://github.com/kir-dev/sch-pincer/blob/46352116c323a946275d1590a769afe553ef81e4/src/main/kotlin/hu/kirdev/schpincer/dto/ItemEntityDto.kt#L8
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public record PincerItem(
    // long Id,
    // string Name,
    // string Description,
    // string Ingredients,
    // string DetailsConfigJson,
    // int Price,
    bool Orderable,
    // bool Service,
    // bool PersonallyOrderable,
    // string ImageName,
    int CircleId,
    // string CircleAlias,
    string CircleName,
    // string CircleColor,
    [property: JsonConverter(typeof(UnixTimeDateTimeConverter)), JsonPropertyName("nextOpeningDate")]
    DateTime? NextOpeningDateUtc,
    // List<TimeWindowEntity> TimeWindows,
    // ItemOrderableStatus OrderStatus,
    // int Flag,
    // string CircleIcon,
    // int CategoryMax,
    // int DiscountPrice,
    // string Keywords,
    bool OutOfStock
);

public class UnixTimeDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        long unixMilliseconds = reader.GetInt64();
        if (unixMilliseconds == 0)
            return null;
        return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) => throw new();
}

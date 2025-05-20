using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        SyncResponse response = (await httpClient.GetFromJsonAsync<SyncResponse>(
            "https://schpincer.sch.bme.hu/api/sync",
            JsonSerializerOptions.Web,
            cancellationToken))!;

        var pincerIds = response.Circles.Select(c => c.Id).ToList();
        var pekIds = response.Circles.Where(c => c.PekId.HasValue).Select(c => c.PekId!.Value).ToList();

        List<Page> pages;
        await using (var tx = await db.BeginTransaction(IsolationLevel.Serializable, cancellationToken))
        {
            pages = await db.Pages
                .Include(p => p.Categories)
                .Where(p => pincerIds.Contains(p.PincerId!.Value) || pekIds.Contains(p.PekId!.Value))
                .ToListAsync(cancellationToken);
            pages = UpdatePages(response.Circles, pages);
            int rowsAffected = await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            if (rowsAffected > 0)
                cache.Remove(CategoryService.CacheKey);
        }

        List<int> openingPincerIds = response.Openings.Select(o => o.Id).ToList();
        List<PincerOpening> currentOpenings = await db.PincerOpenings
            .Where(o => openingPincerIds.Contains(o.PincerId))
            .ToListAsync(cancellationToken);
        Dictionary<int, PincerOpening> pincerIdToOpening = currentOpenings.ToDictionary(o => o.PincerId);
        Dictionary<int, Page> pincerIdToPage = pages.ToDictionary(p => p.PincerId!.Value);

        foreach (OpeningDto incoming in response.Openings)
        {
            if (incoming.CircleId == null)
                return;

            Page page = pincerIdToPage[incoming.CircleId.Value];
            Category defaultCategory = page.Categories[0];

            if (!pincerIdToOpening.TryGetValue(incoming.Id, out PincerOpening? local))
            {
                local = new()
                {
                    PincerId = incoming.Id,
                    CreatedUtc = _utcNow,
                    Title = GetTitle(incoming, page),
                };
                defaultCategory.Events.Add(local);
            }
            else if (!string.IsNullOrWhiteSpace(incoming.Feeling))
                local.Title = GetTitle(incoming, page);

            if (incoming.Description != null)
                local.DescriptionMarkdown = incoming.Description;
            local.StartUtc = incoming.Start;
            local.EndUtc = incoming.End;
            local.OrderingStartUtc = incoming.OrderingStart;
            local.OrderingEndUtc = incoming.OrderingEnd;

            if (!incoming.OutOfStock)
                local.OutOfStockUtc = null;
            else if (!local.OutOfStockUtc.HasValue)
                local.OutOfStockUtc = _utcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        await db.PincerOpenings
            .Where(o => o.StartUtc > _utcNow && !openingPincerIds.Contains(o.PincerId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string GetTitle(OpeningDto opening, Page page)
    {
        if (!string.IsNullOrWhiteSpace(opening.Feeling))
            return opening.Feeling;
        return $"{page.PincerName ?? page.PekName} nyitás";
    }

    private List<Page> UpdatePages(List<Circle> incoming, List<Page> local)
    {
        Dictionary<int, Page> pincerIdToPage = local
            .Where(p => p.PincerId.HasValue)
            .ToDictionary(p => p.PincerId!.Value);
        Dictionary<int, Page> pekIdToPage = local
            .Where(p => p.PekId.HasValue)
            .ToDictionary(p => p.PekId!.Value);

        return incoming
            .Select(circle =>
            {
                Page page = GetPage(circle, pincerIdToPage, pekIdToPage);

                if (page.PincerId != circle.Id)
                {
                    if (page.PincerId == null)
                        page.PincerId = circle.Id;
                    else
                        throw new InvalidOperationException();
                }

                page.PincerName = circle.Name;

                if (circle.PekId != page.PekId)
                {
                    if (!page.PekId.HasValue)
                        page.PekId = circle.PekId;
                    else
                        throw new NotImplementedException(
                            "Updating the PeK ID is not possible if it has been set before");
                }

                return page;
            })
            .ToList();
    }

    private Page GetPage(
        Circle circle,
        Dictionary<int, Page> pincerIdToPage,
        Dictionary<int, Page> pekIdToPage)
    {
        Page? pageFromPincerId = pincerIdToPage.GetValueOrDefault(circle.Id);

        // pek id unknown
        if (!circle.PekId.HasValue)
        {
            // already in db
            if (pageFromPincerId != null)
                return pageFromPincerId;

            // init from pincer
            Page page = new();
            page.Categories.Add(new() { Page = page });
            db.Pages.Add(page);
            return page;
        }

        Page? pageFromPekId = pekIdToPage.GetValueOrDefault(circle.PekId.Value);

        // not in db
        if (pageFromPincerId == null && pageFromPekId == null)
        {
            Page page = new();
            page.Categories.Add(new() { Page = page });
            db.Pages.Add(page);
            return page;
        }

        // added from pek, no pincer id (pek, _)
        if (pageFromPincerId == null)
            return pageFromPekId!;

        // added from pincer, no pek id (_, pincer)
        if (pageFromPekId == null)
            return pageFromPincerId;

        // already in db using the same entity (pek, pincer)
        if (pageFromPincerId == pageFromPekId)
            return pageFromPincerId;

        // added from both pincer and pek as different entities [(pek, _) (_, pincer)]
        return Merge(pageFromPincerId, pageFromPekId);
    }

    private static Page Merge(Page a, Page b)
    {
        throw new NotImplementedException("Merging pages has not been implemented");
        // if (a == b) throw new InvalidOperationException();
        //
        // // 
        //
        // await db.Categories
        //     .Where(c => c.Page == b)
        //     .ExecuteUpdateAsync(x => x.SetProperty(c => c.Page, a));
        //
        // b.Categories.ForEach(c => c.Page = a);
    }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign)]
public record Circle(
    int Id,
    int? PekId,
    string Name
);

[UsedImplicitly(ImplicitUseKindFlags.Assign)]
public record OpeningDto(
    int Id,
    int? CircleId,
    string? Feeling,
    string? Description,
    [property: JsonConverter(typeof(UnixTimeDateTimeConverter))]
    DateTime? Start,
    [property: JsonConverter(typeof(UnixTimeDateTimeConverter))]
    DateTime? End,
    [property: JsonConverter(typeof(UnixTimeDateTimeConverter))]
    DateTime? OrderingStart,
    [property: JsonConverter(typeof(UnixTimeDateTimeConverter))]
    DateTime? OrderingEnd,
    bool OutOfStock
);

public record SyncResponse(
    List<Circle> Circles,
    List<OpeningDto> Openings
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

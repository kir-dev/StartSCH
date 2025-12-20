using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.BackgroundTasks;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.SchPincer;

public class SchPincerPollJob(
    SchPincerModule schPincerModule,
    Db db,
    IMemoryCache cache,
    BackgroundTaskManager backgroundTaskManager,
    HttpClient httpClient)
    : IPollJobExecutor
{
    private readonly DateTime _utcNow = DateTime.UtcNow;

    private static bool _firstRun = true;

    public async Task Execute(CancellationToken cancellationToken)
    {
        SyncResponse response = (await httpClient.GetFromJsonAsync<SyncResponse>(
            "https://schpincer.sch.bme.hu/api/sync",
            Utils.JsonSerializerOptions,
            cancellationToken))!;

        if (_firstRun)
        {
            _firstRun = false;

            if (!await db.PincerOpenings.AnyAsync(cancellationToken))
            {
                List<OpeningResponse> endedOpenings = (await httpClient.GetFromJsonAsync<List<OpeningResponse>>(
                    $"https://schpincer.sch.bme.hu/api/openings/ended?before={new DateTimeOffset(_utcNow).ToUnixTimeMilliseconds()}&count=100000",
                    Utils.JsonSerializerOptions,
                    cancellationToken
                ))!;
                
                // only add openings that were not returned by /sync
                HashSet<int> usedIds = response.Openings.Select(o => o.Id).ToHashSet();
                response.Openings.AddRange(endedOpenings.Where(o => !usedIds.Contains(o.Id)));
            }
        }

        // ignore openings without at least a start time
        response = response with { Openings = response.Openings.Where(o => o.Start.HasValue).ToList() };

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
                cache.Remove(InterestService.CacheKey);
        }

        List<int> openingPincerIds = response.Openings.Select(o => o.Id).ToList();
        List<PincerOpening> currentOpenings = await db.PincerOpenings
            .Where(o => openingPincerIds.Contains(o.PincerId))
            .Include(x => x.CreateOrderingStartedNotifications)
            .ToListAsync(cancellationToken);
        Dictionary<int, PincerOpening> pincerIdToOpening = currentOpenings.ToDictionary(o => o.PincerId);
        Dictionary<int, Page> pincerIdToPage = pages.ToDictionary(p => p.PincerId!.Value);

        foreach (OpeningResponse incoming in response.Openings)
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
                    Title = GetTitle(incoming, page),
                };
                if (incoming.OrderingEnd > _utcNow)
                    local.CreateOrderingStartedNotifications = new() { Created = _utcNow, };
                defaultCategory.Events.Add(local);
            }
            else if (!string.IsNullOrWhiteSpace(incoming.Feeling))
                local.Title = GetTitle(incoming, page);

            if (incoming.Description != null)
                local.DescriptionMarkdown = incoming.Description;
            local.Start = incoming.Start;
            local.End = incoming.End;
            local.OrderingStart = incoming.OrderingStart;
            local.OrderingEnd = incoming.OrderingEnd;

            if (local.CreateOrderingStartedNotifications != null)
                local.CreateOrderingStartedNotifications.WaitUntil = local.OrderingStart;

            if (incoming.OutOfStock == false)
                local.OutOfStock = null;
            else if (!local.OutOfStock.HasValue && incoming.OutOfStock == true)
                local.OutOfStock = _utcNow;
        }

        int rowsAffected2 = await db.SaveChangesAsync(cancellationToken);
        rowsAffected2 += await db.PincerOpenings
            .Where(o => o.Start > _utcNow && !openingPincerIds.Contains(o.PincerId))
            .ExecuteDeleteAsync(cancellationToken);
        if (rowsAffected2 > 0)
            backgroundTaskManager.Notify();
    }

    private static string GetTitle(OpeningResponse opening, Page page)
    {
        if (!string.IsNullOrWhiteSpace(opening.Feeling))
            return opening.Feeling;
        return $"{page.GetName()} nyitás";
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
            return CreatePage();
        }

        Page? pageFromPekId = pekIdToPage.GetValueOrDefault(circle.PekId.Value);

        // not in db
        if (pageFromPincerId == null && pageFromPekId == null)
            return CreatePage();

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

    private Page CreatePage()
    {
        Page page = new();
        page.Categories.Add(new()
        {
            Page = page,
            IncluderCategoryIncludes =
            {
                new()
                {
                    IncluderId = schPincerModule.DefaultCategoryId,
                },
            },
            Interests =
            {
                new EmailWhenOrderingStartedInCategory(),
                new EmailWhenPostPublishedInCategory(),
                new PushWhenOrderingStartedInCategory(),
                new PushWhenPostPublishedInCategory(),
                new ShowEventsInCategory(),
                new ShowPostsInCategory(),
            },
        });
        db.Pages.Add(page);
        return page;
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
public record OpeningResponse(
    int Id,
    int? CircleId,
    string? Feeling,
    string? Description,
    [property: JsonConverter(typeof(UnixTimeMillisecondsInstantJsonConverter))]
    Instant? Start,
    [property: JsonConverter(typeof(UnixTimeMillisecondsInstantJsonConverter))]
    Instant? End,
    [property: JsonConverter(typeof(UnixTimeMillisecondsInstantJsonConverter))]
    Instant? OrderingStart,
    [property: JsonConverter(typeof(UnixTimeMillisecondsInstantJsonConverter))]
    Instant? OrderingEnd,
    bool? OutOfStock
);

public record SyncResponse(
    List<Circle> Circles,
    List<OpeningResponse> Openings
);

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
        SyncResponse response = (await httpClient.GetFromJsonAsync<SyncResponse>("https://schpincer.sch.bme.hu/api/sync", JsonSerializerOptions.Web, cancellationToken))!;
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var pincerIds = response.Circles.Select(c => c.Id).ToList();
        var pekIds = response.Circles.Where(c => c.PekId.HasValue).Select(c => c.PekId!.Value).ToList();
        List<Page> pages = await db.Pages
            .Where(p => pincerIds.Contains(p.PincerId!.Value) || pekIds.Contains(p.PekId!.Value))
            .ToListAsync(cancellationToken);
        UpdatePages(response.Circles, pages);
    }

    private static void UpdatePages(List<Circle> incoming, List<Page> stored)
    {
        Dictionary<int, Page> pincerIdToPage = stored
                .Where(p => p.PincerId.HasValue)
                .ToDictionary(p => p.PincerId!.Value);
        Dictionary<int, Page> pekIdToPage = stored
            .Where(p => p.PekId.HasValue)
            .ToDictionary(p => p.PekId!.Value);
        
        foreach (Circle circle in incoming)
        {
            // 1. already in db using the same entity (pek, pincer)
            // 2. added from pek, no pincer id (pek, _)
            // 3. added from pincer, no pek id (_, pincer)
            // 4. added from both pincer and pek as different entities [(pek, _) (_, pincer)]
            // 5. not in db []

            Page page;
            
            Page? pageFromPincerId = pincerIdToPage.GetValueOrDefault(circle.Id);

            if (circle.PekId.HasValue)
            {
                Page? pageFromPekId = pekIdToPage.GetValueOrDefault(circle.PekId.Value);
                
                
            }
            else
            {
                
            }
        }
    }
}

[UsedImplicitly(ImplicitUseKindFlags.Assign)]
public record Circle(
    int Id,
    int? PekId,
    string Name
);

[UsedImplicitly(ImplicitUseKindFlags.Assign)]
public record Opening(
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
    bool? OutOfStock
);

public record SyncResponse(
    List<Circle> Circles,
    List<Opening> Openings
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

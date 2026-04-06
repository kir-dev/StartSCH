using Ical.Net;
using Microsoft.Extensions.Caching.Memory;

namespace StartSch.Services;

public class IcalendarCache(
    IMemoryCache memoryCache,
    HttpClient httpClient
)
{
    public async Task<List<FullCalendarEvent>> GetEvents(string url)
    {
        return await memoryCache.GetOrCreateAsync(
                "ical " + url,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                    string s = await httpClient.GetStringAsync(url);
                    var cal = Calendar.Load(s);
                    var res = cal.Events
                        .Select(e => new FullCalendarEvent(
                            e.Uid ?? throw new NotImplementedException(),
                            new(e.Start.AsUtc),
                            new(e.End.AsUtc),
                            e.Summary,
                            "#000",
                            "#fff"
                        ))
                        .ToList();
                    return res;
                });
    }
}

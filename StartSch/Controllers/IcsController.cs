using System.Text;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StartSch.Data;

namespace StartSch.Controllers;

[ApiController]
public class IcsController(
    Db db,
    IOptions<StartSchOptions> options,
    IMemoryCache cache)
    : ControllerBase
{
    [HttpGet("/calendars/everything.ics")]
    public async Task<string> GetEverythingIcs()
    {
        return (await cache.GetOrCreateAsync("everything.ics", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            Calendar calendar = new()
            {
                Properties = { new CalendarProperty("X-WR-CALNAME", "StartSCH") }
            };
            calendar.Events.AddRange(
                (await db.Events.ToListAsync())
                .Where(e => e.Start.HasValue)
                .Select(e =>
                {
                    string? description = !string.IsNullOrWhiteSpace(e.DescriptionMarkdown)
                        ? new TextContent(e.DescriptionMarkdown, null).HtmlContent
                        : null;
                    if (description == "") description = null;

                    string startSchUrl = $"{options.Value.PublicUrl}/events/{e.Id}";

                    StringBuilder sb = new();

                    if (description != null)
                        sb.Append("<p>");

                    sb.Append("<a href=\"");
                    sb.Append(startSchUrl);
                    sb.Append("?utm_medium=ics");
                    sb.Append("\">StartSCH</a>");
                    if (e.ExternalUrl != null && Uri.TryCreate(e.ExternalUrl, UriKind.Absolute, out Uri? uri))
                    {
                        sb.Append(" | <a href=\"");
                        sb.Append(e.ExternalUrl);
                        sb.Append("\">");
                        sb.Append(uri.Host);
                        sb.Append("</a>");
                    }

                    if (description != null)
                    {
                        sb.Append("</p>");
                        sb.Append("<hr>");
                        sb.Append(description);
                    }

                    CalDateTime startCal;
                    CalDateTime? endCal;
                    if (e.AllDay)
                    {
                        var (start, end) = Utils.AllDayGetDates(e.Start!.Value, e.End);
                        startCal = new(start);
                        endCal = new(end);
                    }
                    else
                    {
                        startCal = new(e.Start!.Value);
                        endCal = e.End.HasValue ? new(e.End.Value) : null;
                    }

                    return new CalendarEvent()
                    {
                        Uid = startSchUrl,
                        Url = new(startSchUrl, UriKind.Absolute),
                        Start = startCal,
                        End = endCal,
                        DtStamp = new CalDateTime(e.Updated),
                        Summary = e.Title,
                        Description = sb.ToString(),
                    };
                })
            );

            return new CalendarSerializer().SerializeToString(calendar)!;
        }))!;
    }
}

using System.Text;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;

namespace StartSch.Controllers;

[ApiController]
public class IcsController(Db db, IOptions<StartSchOptions> options) : ControllerBase
{
    [HttpGet("/temp/everything.ics")]
    public async Task<string> GetEverythingIcs()
    {
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

                StringBuilder sb = new();

                if (description != null)
                    sb.Append("<p>");

                sb.Append("<a href=\"");
                sb.Append(options.Value.PublicUrl);
                sb.Append("/events/");
                sb.Append(e.Id);
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

                return new CalendarEvent()
                {
                    Start = new CalDateTime(e.Start!.Value),
                    End = e.End.HasValue ? new CalDateTime(e.End.Value) : null,
                    Summary = e.Title,
                    Description = sb.ToString(),
                };
            })
        );

        return new CalendarSerializer().SerializeToString(calendar)!;
    }
}

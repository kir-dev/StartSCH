using System.Text;
using System.Text.Json;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StartSch.Data;
using StartSch.Services;
using StartSch.Wasm;

namespace StartSch.Controllers;

[ApiController]
public class IcsController(
    Db db,
    IOptions<StartSchOptions> options,
    IMemoryCache cache,
    IcalendarCache calendarCache,
    IDataProtectionProvider dataProtectionProvider)
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
                        (LocalDate start, LocalDate end) = Utils.AllDayGetDates(e.Start!.Value, e.End);
                        startCal = new(start.ToDateOnly());
                        endCal = new(end.ToDateOnly());
                    }
                    else
                    {
                        startCal = new(e.Start!.Value.ToDateTimeUtc());
                        endCal = e.End.HasValue ? new(e.End.Value.ToDateTimeUtc()) : null;
                    }

                    return new CalendarEvent()
                    {
                        Uid = startSchUrl,
                        Url = new(startSchUrl, UriKind.Absolute),
                        Start = startCal,
                        End = endCal,
                        DtStamp = new(e.Updated.ToDateTimeUtc()),
                        Summary = e.Title,
                        Description = sb.ToString(),
                    };
                })
            );

            return new CalendarSerializer().SerializeToString(calendar)!;
        }))!;
    }

    [HttpGet("/calendars/personal/{calendarId:int}.ics")]
    public async Task<ActionResult> GetPersonalCalendarIcs(int calendarId)
    {
        string? key = Request.Query["key"];
        if (string.IsNullOrEmpty(key))
            return BadRequest("Missing key parameter");

        (byte[] aesKey, int protectedCalendarId) =
            PersonalCalendarExportUrlExtensions.UnprotectIcsKey(key, dataProtectionProvider);
        if (protectedCalendarId != calendarId)
            return NotFound();

        return Content(
            (await cache.GetOrCreateAsync($"personal-ics-{calendarId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var cal = await db.PersonalStartSchCalendars
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == calendarId);
                if (cal == null)
                    return "";

                string? configJson = cal.User.PersonalCalendarConfiguration;

                var externalCalendars = await db.ExternalPersonalCalendars
                    .Where(c => c.UserId == cal.UserId)
                    .ToListAsync();

                var allEvents = new List<PersonalCalendarEvent>();
                foreach (var ec in externalCalendars)
                {
                    try
                    {
                        string url = ec.GetUrl(aesKey);
                        var events = await calendarCache.GetEvents(url);
                        allEvents.AddRange(events);
                    }
                    catch
                    {
                    }
                }

                IEnumerable<PersonalCalendarEvent> calendarEvents = [];
                if (configJson != null)
                {
                    var config = JsonSerializer.Deserialize<PersonalCalendarConfigurationDto>(
                        configJson, SharedUtils.JsonSerializerOptionsWebWithNodaTime)!;

                    var relevantMods = config.Modifications
                        .Where(m => m.Action is CategoryModification { NewCategoryId: var catId } && catId == calendarId);

                    var includedEventKeys = new HashSet<(string, string, Instant)>();
                    foreach (var mod in relevantMods)
                    {
                        if (mod.Target is NeptunSeriesTarget seriesTarget)
                        {
                            foreach (var date in seriesTarget.SelectedDates)
                                includedEventKeys.Add((
                                    seriesTarget.SubjectAndCourse.Subject,
                                    seriesTarget.SubjectAndCourse.Course,
                                    date));
                        }
                    }

                    calendarEvents = allEvents
                        .Where(e => e.Subject != null && e.Course != null &&
                                    includedEventKeys.Contains((e.Subject, e.Course, e.Start)));
                }

                var icalCalendar = new Calendar
                {
                    Properties = { new CalendarProperty("X-WR-CALNAME", cal.Name) }
                };
                icalCalendar.Events.AddRange(
                    calendarEvents.Select(e =>
                    {
                        string uid = $"{calendarId}-{e.Id}@{options.Value.PublicUrl}";
                        return new CalendarEvent()
                        {
                            Uid = uid,
                            Start = new CalDateTime(e.Start.ToDateTimeUtc()),
                            End = new CalDateTime(e.End.ToDateTimeUtc()),
                            Summary = e.Title,
                            Location = e.Location,
                        };
                    })
                );

                return new CalendarSerializer().SerializeToString(icalCalendar)!;
            }))!,
            "text/calendar; charset=utf-8");
    }
}

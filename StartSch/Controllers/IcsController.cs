using System.Text;
using System.Text.Json;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StartSch.Data;
using StartSch.Services;
using StartSch.Wasm;
using StartSch.Wasm.Components;
using StartSch.Wasm.PersonalCalendars;

namespace StartSch.Controllers;

[ApiController]
public class IcsController(
    Db db,
    IOptions<StartSchOptions> options,
    IMemoryCache cache,
    PersonalCalendarService personalCalendarService,
    IDataProtectionProvider dataProtectionProvider,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory)
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

    [HttpGet("/calendars/personal/{categoryId:int}.ics")]
    public async Task<ActionResult<string>> GetPersonalCalendarCategoryIcs(int categoryId, string token)
    {
        var requestToken = PersonalCalendarCategoryRequestToken.Deserialize(token, dataProtectionProvider);
        if (requestToken.CategoryId != categoryId)
            return $"{nameof(categoryId)} does not match category ID in {nameof(token)}";

        var category = await db.PersonalCalendarCategories
            .Include(c => c.User)
            .ThenInclude(u => u.DefaultPersonalCalendarCategory)
            .Include(c => c.User)
            .ThenInclude(u => u.DefaultPersonalCalendarExamCategory)
            .FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category == null)
            return "Category not found";
        var user = category.User;

        PersonalCalendarContextDto contextDto = await personalCalendarService.GetContextDto(user, requestToken.AesKey);
        PersonalCalendarContext context = new(contextDto);
        var events = context.GetEventsInCategory(categoryId);

        Calendar calendar = new()
        {
            Properties = { new CalendarProperty("X-WR-CALNAME", $"StartSCH | {category.Name}") },
        };
        var publicUrl = options.Value.PublicUrl;
        var editorToken = new PersonalCalendarEditorToken(user.Id, requestToken.AesKey)
            .Serialize(dataProtectionProvider);

        await using var scope = serviceProvider.CreateAsyncScope();
        await using var htmlRenderer = new HtmlRenderer(scope.ServiceProvider, loggerFactory);
        await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await htmlRenderer.RenderComponentAsync<PersonalCalendarEventDescription>();
            var s = root.ToHtmlString();
            calendar.Events.AddRange(
                events.Select(e =>
                {
                    var originalEvent = e.OriginalEvent;
                    var modifiedEvent = e.ModifiedEvent;
                    return new CalendarEvent
                    {
                        Uid = $"{modifiedEvent.SourceCalendar.Id}/{modifiedEvent.Id}@{publicUrl}",
                        Start = new(modifiedEvent.Start.ToDateTimeUtc()),
                        End = new(modifiedEvent.End.ToDateTimeUtc()),
                        Summary = modifiedEvent.Title,
                        Description = s,
                    };
                })
            );
        });

        return Content(
            new CalendarSerializer().SerializeToString(calendar)!,
            "text/calendar; charset=utf-8"
        );
    }
}

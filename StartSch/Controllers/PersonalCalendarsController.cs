using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;
using StartSch.Services;
using StartSch.Wasm.PersonalCalendars;

namespace StartSch.Controllers;

[ApiController, Route("/calendars/personal")]
public class PersonalCalendarsController(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<StartSchOptions> startSchOptions,
    IcalendarCache icalendarCache,
    Db db
) : ControllerBase
{
    [HttpPut, Authorize]
    public async Task<object> CreateOrUpdate(PersonalCalendarLive request, string token)
    {
        int userId = User.GetId();
        PersonalCalendar? personalCalendar;
        if (request.Id != 0)
        {
            personalCalendar = await db.PersonalCalendars.FirstOrDefaultAsync(x => x.Id == request.Id);
            if (personalCalendar == null)
                return NotFound();
            if (personalCalendar.UserId != userId)
                return Unauthorized();
        }
        else
        {
            personalCalendar = request switch
            {
                PersonalCalendarCategoryLive => new PersonalCalendarCategory(),
                PersonalNeptunCalendarLive => new PersonalNeptunCalendar(),
                PersonalMoodleCalendarLive => new PersonalMoodleCalendar(),
                _ => throw new InvalidOperationException(),
            };
            personalCalendar.UserId = userId;
            db.PersonalCalendars.Add(personalCalendar);
        }

        personalCalendar.Name = request.Name;

        var editorToken = PersonalCalendarEditorToken.Deserialize(token, dataProtectionProvider);
        if (editorToken.UserId != userId)
            return Unauthorized("Encryption token belongs to a different user");

        switch (request)
        {
            case ExternalPersonalCalendarLive externalPersonalCalendarRequest:
            {
                ExternalPersonalCalendar externalCalendar = (ExternalPersonalCalendar)personalCalendar;
                var oldUrl = externalCalendar.GetUrl(editorToken.AesKey);
                var newUrl = externalPersonalCalendarRequest.Url;
                if (oldUrl == newUrl)
                    break;
                externalCalendar.SetUrl(newUrl, editorToken.AesKey);
                request.Events = Uri.IsWellFormedUriString(newUrl, UriKind.Absolute)
                    ? await icalendarCache.GetEvents(newUrl, request.GetType())
                    : [];
                break;
            }
            case PersonalCalendarCategoryLive liveCategory:
            {
                var category = (PersonalCalendarCategory)personalCalendar;
                category.Color = liveCategory.Color is { Length: > 0 } colorString && uint.TryParse(
                    colorString.AsSpan(1),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out var color)
                    ? color
                    : 0;
                liveCategory.IcsUrl ??=
                    $"{startSchOptions.Value.PublicUrl}/calendars/personal/{category.Id}.ics?token={
                        new PersonalCalendarCategoryRequestToken(category.Id, editorToken.AesKey)
                            .Serialize(dataProtectionProvider)
                    }";
                break;
            }
        }

        await db.SaveChangesAsync();

        request.Id = personalCalendar.Id;
        return TypedResults.Json(request);
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        int userId = User.GetId();
        PersonalCalendar? personalCalendar = await db.PersonalCalendars.FirstOrDefaultAsync(x => x.Id == id);
        if (personalCalendar == null)
            return NotFound();
        if (personalCalendar.UserId != userId)
            return Unauthorized();
        db.PersonalCalendars.Remove(personalCalendar);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("config"), Authorize]
    public async Task<IActionResult> UpdateConfig(PersonalCalendarConfigurationDto config)
    {
        int userId = User.GetId();
        User user = await db.Users.FirstAsync(u => u.Id == userId);
        user.PersonalCalendarConfiguration =
            JsonSerializer.Serialize(config, SharedUtils.JsonSerializerOptionsWebWithNodaTime);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

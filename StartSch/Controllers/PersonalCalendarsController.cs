using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Data;

namespace StartSch.Controllers;

[ApiController, Route("/calendars/personal")]
public class PersonalCalendarsController(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<StartSchOptions> startSchOptions,
    Db db
) : ControllerBase
{
    [HttpPut, Authorize]
    public async Task<object> CreateOrUpdate(
        PersonalCalendarLive request,
        [FromQuery(Name = "key")] string? protectedEncryptionToken
    )
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

        switch (request)
        {
            case ExternalPersonalCalendarLive externalPersonalCalendarRequest:
            {
                ArgumentNullException.ThrowIfNull(protectedEncryptionToken);
                PersonalCalendarEncryptionToken encryptionToken =
                    PersonalCalendarEncryptionToken.Deserialize(protectedEncryptionToken, dataProtectionProvider);
                if (encryptionToken.UserId != userId)
                    return Unauthorized("Encryption token belongs to a different user");

                ExternalPersonalCalendar externalPersonalCalendar = (ExternalPersonalCalendar)personalCalendar;
                externalPersonalCalendar.SetUrl(externalPersonalCalendarRequest.Url, encryptionToken.AesKey);
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
                break;
            }
        }

        await db.SaveChangesAsync();

        if (request.Id != 0)
            return NoContent();

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

    [HttpGet("{id:int}/ics-url"), Authorize]
    public async Task<ActionResult<string>> GetIcsUrl(
        int id,
        [FromQuery(Name = "key")] string protectedEncryptionToken)
    {
        int userId = User.GetId();

        PersonalCalendarCategory? category =
            await db.PersonalCalendarCategories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (category == null)
            return NotFound();

        PersonalCalendarEncryptionToken encryptionToken =
            PersonalCalendarEncryptionToken.Deserialize(protectedEncryptionToken, dataProtectionProvider);
        if (encryptionToken.UserId != userId)
            return Unauthorized("Encryption token belongs to a different user");

        return Ok(PersonalCalendarExportUrlExtensions.GenerateIcsUrl(
            id, encryptionToken.AesKey, startSchOptions.Value.PublicUrl, dataProtectionProvider));
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

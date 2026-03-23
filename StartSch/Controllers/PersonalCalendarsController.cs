using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Controllers;

[ApiController, Route("/calendars/personal")]
public class PersonalCalendarsController(
    IDataProtectionProvider dataProtectionProvider,
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
                PersonalStartSchCalendarLive => new PersonalStartSchCalendar(),
                PersonalNeptunCalendarLive => new PersonalNeptunCalendar(),
                PersonalMoodleCalendarLive => new PersonalMoodleCalendar(),
                _ => throw new InvalidOperationException(),
            };
            personalCalendar.UserId = userId;
            db.PersonalCalendars.Add(personalCalendar);
        }

        personalCalendar.Name = request.Name;

        if (request is ExternalPersonalCalendarLive externalPersonalCalendarRequest)
        {
            ArgumentNullException.ThrowIfNull(protectedEncryptionToken);
            PersonalCalendarEncryptionToken encryptionToken =
                PersonalCalendarEncryptionToken.Unprotect(protectedEncryptionToken, dataProtectionProvider);
            if (encryptionToken.UserId != userId)
                return Unauthorized("Encryption token belongs to a different user");

            ExternalPersonalCalendar externalPersonalCalendar = (ExternalPersonalCalendar)personalCalendar;
            externalPersonalCalendar.SetUrl(externalPersonalCalendarRequest.Url, encryptionToken.AesKey);
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

    [HttpPost("reset-encryption-key"), Authorize]
    public async Task<ActionResult<ResetEncryptionKeyResult>> ResetEncryptionKey()
    {
        int userId = User.GetId();
        await db.ExternalPersonalCalendars
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync();
        await db.PersonalCalendarExports
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync();
        byte[] aesKey = Crypto.GenerateAesEncryptionKey();
        return new ResetEncryptionKeyResult(
            new PersonalCalendarEncryptionToken(aesKey, userId).Protect(dataProtectionProvider)
        );
    }
}

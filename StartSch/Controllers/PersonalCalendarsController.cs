using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Wasm;

namespace StartSch.Controllers;

[ApiController, Route("/calendars/personal")]
public class PersonalCalendarsController(
    IDataProtectionProvider dataProtectionProvider,
    Db db
) : ControllerBase
{
    [HttpPost("decrypt-encryption-key"), Authorize]
    public ActionResult<PersonalCalendarEncryptionToken> DecryptEncryptionKey([FromBody] string ciphertext)
    {
        int userId = User.GetId();
        var encryptionKey = PersonalCalendarEncryptionToken.Unprotect(ciphertext, dataProtectionProvider);
        if (userId != encryptionKey.UserId)
            return Unauthorized();
        return encryptionKey;
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

    [HttpPut, Authorize]
    public async Task<ActionResult<PersonalCalendarLive>> CreateOrUpdate(
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
        }

        personalCalendar.UserId = userId;
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

        db.PersonalCalendars.Add(personalCalendar);
        await db.SaveChangesAsync();

        request.Id = personalCalendar.Id;

        return Created($"/calendars/personal/{request.Id}", request);
    }
}

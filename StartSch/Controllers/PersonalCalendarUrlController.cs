using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Wasm;

namespace StartSch.Controllers;

public class PersonalCalendarUrlController(
    IDataProtectionProvider dataProtectionProvider,
    Db db
) : ControllerBase
{
    [HttpPost("/calendars/personal/decrypt-encryption-key"), Authorize]
    public ActionResult<PersonalCalendarEncryptionToken> DecryptEncryptionKey([FromBody] string ciphertext)
    {
        int userId = User.GetId();
        var encryptionKey = PersonalCalendarEncryptionToken.Unprotect(ciphertext, dataProtectionProvider);
        if (userId != encryptionKey.UserId)
            return Unauthorized();
        return encryptionKey;
    }

    [HttpPost, Authorize]
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
            aesKey,
            new PersonalCalendarEncryptionToken(aesKey, userId).Protect(dataProtectionProvider)
        );
    }
}

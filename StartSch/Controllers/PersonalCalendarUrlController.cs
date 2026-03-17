using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using StartSch.Data;
using StartSch.Wasm;

namespace StartSch.Controllers;

public class PersonalCalendarUrlController(
    IDataProtectionProvider dataProtectionProvider,
    Db db
) : ControllerBase
{
    [Authorize]
    [HttpPost("/calendars/personal/decrypt-encryption-key")]
    public ActionResult<PersonalCalendarEncryptionKey> DecryptEncryptionKey([FromBody] string ciphertext)
    {
        int userId = User.GetId();
        var encryptionKey = PersonalCalendarEncryptionKey.Unprotect(ciphertext, dataProtectionProvider);
        if (userId != encryptionKey.UserId)
            return Unauthorized();
        return encryptionKey;
    }
    //
    // [Authorize]
    // [HttpPost]
    // public IActionResult<
}

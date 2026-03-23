using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using StartSch.Wasm;

namespace StartSch;

public static class PersonalCalendarEncryptionKeyExtensions
{
    private const string DataProtectionPurpose = "StartSch.PersonalCalendarEncryptionToken";

    extension(PersonalCalendarEncryptionToken personalCalendarEncryptionToken)
    {
        public string Protect(IDataProtectionProvider dataProtectionProvider)
        {
            var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
            return protector.Protect(JsonSerializer.Serialize(personalCalendarEncryptionToken));
        }
        
        public static PersonalCalendarEncryptionToken Unprotect(
            string protectedString,
            IDataProtectionProvider dataProtectionProvider)
        {
            var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
            return JsonSerializer.Deserialize<PersonalCalendarEncryptionToken>(protector.Unprotect(protectedString))!;
        }
    }
}

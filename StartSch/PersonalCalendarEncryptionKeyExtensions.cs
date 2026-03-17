using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using StartSch.Wasm;

namespace StartSch;

public static class PersonalCalendarEncryptionKeyExtensions
{
    private const string DataProtectionPurpose = "StartSch.PersonalCalendarEncryptionKey";

    extension(PersonalCalendarEncryptionKey personalCalendarEncryptionKey)
    {
        public string Protect(IDataProtectionProvider dataProtectionProvider)
        {
            var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
            return protector.Protect(JsonSerializer.Serialize(personalCalendarEncryptionKey));
        }
        
        public static PersonalCalendarEncryptionKey Unprotect(
            string protectedString,
            IDataProtectionProvider dataProtectionProvider)
        {
            var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
            return JsonSerializer.Deserialize<PersonalCalendarEncryptionKey>(protector.Unprotect(protectedString))!;
        }
    }
}

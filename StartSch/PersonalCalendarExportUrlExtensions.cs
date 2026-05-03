using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace StartSch;

public static class PersonalCalendarExportUrlExtensions
{
    private const string DataProtectionPurpose = "StartSch.PersonalCalendarExportUrl";

    public static string GenerateIcsUrl(
        // TODO: rename to categoryId
        int calendarId,
        ReadOnlySpan<byte> aesKey,
        string publicUrl,
        IDataProtectionProvider dataProtectionProvider)
    {
        var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
        string protectedKey = protector.Protect(JsonSerializer.Serialize(new IcsKeyData(aesKey.ToArray(), calendarId)));
        return $"{publicUrl}/calendars/personal/{calendarId}.ics?key={protectedKey}";
    }

    public static (byte[] AesKey, int CalendarId) UnprotectIcsKey(
        string protectedKey,
        IDataProtectionProvider dataProtectionProvider)
    {
        var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
        var data = JsonSerializer.Deserialize<IcsKeyData>(protector.Unprotect(protectedKey))!;
        return (data.AesKey, data.CalendarId);
    }

    private record IcsKeyData(
        byte[] AesKey,
        int CalendarId
    );
}

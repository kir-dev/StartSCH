using System.Buffers.Text;
using System.Text.Json;
using System.Web;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Extensions;
using StartSch.Wasm;

namespace StartSch;

public static class PersonalCalendarExportUrlExtensions
{
    private const string DataProtectionPurpose = "StartSch.PersonalCalendarExportUrl";

    extension(PersonalCalendarExportUrl personalCalendarExportUrl)
    {
        public string Serialize(string StartSchUrl, IDataProtectionProvider dataProtectionProvider)
        {
            var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);

            UriBuilder uriBuilder = new($"{StartSchUrl}/calendars/personal/{personalCalendarExportUrl.ExportId}.ics");

            QueryBuilder fragment = new();
            if (personalCalendarExportUrl.EventId != null)
                fragment.Add("event", personalCalendarExportUrl.EventId);
            fragment.Add(
                "key",
                protector.Protect(
                    JsonSerializer.Serialize(
                        new ProtectedData(
                            personalCalendarExportUrl.AesKey,
                            personalCalendarExportUrl.ExportId
                        )
                    )
                )
            );

            uriBuilder.Fragment = fragment.ToString();

            return uriBuilder.Uri.ToString();
        }

        public static PersonalCalendarExportUrl Deserialize(string url, IDataProtectionProvider dataProtectionProvider)
        {
            var protector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);

            Uri uri = new(url);

            var query = HttpUtility.ParseQueryString(uri.Fragment[1..]);
            (byte[] aesKey, int protectedDataExportId) = JsonSerializer.Deserialize<ProtectedData>(
                protector.Unprotect(
                    Base64Url.DecodeFromChars(
                        query["key"]!
                    )
                )
            )!;
            int urlExportId = int.Parse(uri.Segments[3].RemoveFromEnd(".ics"));
            if (protectedDataExportId != urlExportId) throw new InvalidOperationException("Export ID mismatch");
            return new()
            {
                AesKey = aesKey,
                ExportId = protectedDataExportId,
                EventId = query.GetValues("event") is [{ } eventId]
                    ? eventId
                    : null
            };
        }
    }

    private record ProtectedData(
        [UsedImplicitly] byte[] AesKey,
        int ExportId
    );
}

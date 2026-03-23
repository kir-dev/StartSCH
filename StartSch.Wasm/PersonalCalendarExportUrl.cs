using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Web;

namespace StartSch.Wasm;

public class PersonalCalendarExportUrl
{
    public required int ExportId { get; set; }
    public required byte[] AesKey { get; set; }
    public string? EventId { get; set; }

    public static bool TryParse(
        string exportUrl, string startSchPublicUrl,
        [NotNullWhen(false)] out string? errorMessage,
        [NotNullWhen(true)] out byte[]? protectedData,
        out string? eventId,
        [NotNullWhen(true)] out int? exportId
    )
    {
        errorMessage = null;
        protectedData = null;
        eventId = null;
        exportId = 0;

        exportUrl = exportUrl.Trim();

        if (!exportUrl.StartsWith(startSchPublicUrl))
        {
            errorMessage = $"Must start with {startSchPublicUrl}";
            return false;
        }

        if (!Uri.TryCreate(exportUrl, UriKind.Absolute, out Uri? uri))
        {
            errorMessage = "Invalid URL";
            return false;
        }

        var query = HttpUtility.ParseQueryString(uri.Query);

        var keys = query.GetValues("key");
        if (keys is not [{ } key])
        {
            errorMessage = "Missing key";
            return false;
        }

        try
        {
            protectedData = Base64Url.DecodeFromChars(key);
        }
        catch (FormatException exception)
        {
            errorMessage = exception.Message;
            return false;
        }

        eventId = query.GetValues("event") is [{ } eventId1]
            ? eventId1
            : null;
        exportId = int.Parse(uri.Segments[3].RemoveFromEnd(".ics"));
        return true;
    }
}

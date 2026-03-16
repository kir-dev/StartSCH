using System.Buffers.Text;
using System.Web;

namespace StartSch.Wasm;

public class PersonalCalendarExportUrl
{
    public required int ExportId { get; set; }
    public required byte[] AesKey { get; set; }
    public string? EventId { get; set; }

    public static string? TryParse(
        string exportUrl, string startSchPublicUrl,
        out byte[] protectedData, out string? eventId, out int exportId
    )
    {
        exportUrl = exportUrl.Trim();
        if (!exportUrl.StartsWith(startSchPublicUrl))
            throw new();
        Uri uri = new(exportUrl);
        var query = HttpUtility.ParseQueryString(uri.Fragment[1..]);
        protectedData = Base64Url.DecodeFromChars(
            query["key"]!
        );
        eventId = query.GetValues("event") is [{ } eventId1]
            ? eventId1
            : null;
        exportId = int.Parse(uri.Segments[3].RemoveFromEnd(".ics"));
        return null;
    }
}

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Web;
using Microsoft.AspNetCore.Http.Extensions;

namespace StartSch;

public static class Crypto
{
    public static byte[] GenerateAesEncryptionKey()
    {
        return RandomNumberGenerator.GetBytes(32);
    }
    
}

public class PersonalCalendarUrl
{
    public required int CalendarId { get; set; }
    public required byte[] AesKey { get; set; }
    public string? EventId { get; set; }
    
    public string Serialize(string StartSchUrl)
    {
        UriBuilder uriBuilder = new($"{StartSchUrl}/calendars/personal/{CalendarId}");
        
        QueryBuilder fragment = new();
        if (EventId != null)
            fragment.Add("event", EventId);
        fragment.Add("key", Base64Url.EncodeToString(AesKey));

        uriBuilder.Fragment = fragment.ToString();

        return uriBuilder.Uri.ToString();
    }

    public static PersonalCalendarUrl Deserialize(string url)
    {
        Uri uri = new(url);
        
        var query = HttpUtility.ParseQueryString(uri.Fragment[1..]);
        return new()
        {
            AesKey = Base64Url.DecodeFromChars(query["key"]!),
            CalendarId = int.Parse(uri.Segments[2]),
            EventId = query.GetValues("event") is [{ } eventId]
                ? eventId
                : null
        };
    }
}

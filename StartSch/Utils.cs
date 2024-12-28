using System.Globalization;
using System.Text;
using System.Text.Json;
using Markdig;

namespace StartSch;

public static class Utils
{
    // TODO: remove when updating to .NET 9
    public static JsonSerializerOptions JsonSerializerOptionsWeb { get; } = new(JsonSerializerDefaults.Web);

    public static CultureInfo HungarianCulture { get; } = new("hu-HU");
    public static TimeZoneInfo HungarianTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");

    // Used to match Pek names to Pincer names
    public static bool RoughlyMatches(this string a, string b)
    {
        a = a.Simplify();
        b = b.Simplify();
        return a.Contains(b) || b.Contains(a);
    }

    // Lowercase, remove diacritics, remove symbols
    public static string Simplify(this string str)
    {
        // TODO: use spans to avoid allocation
        str = str.ToLower(HungarianCulture);
        StringBuilder sb = new(str.Length);
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i] switch
            {
                'á' => 'a',
                'é' => 'e',
                'í' => 'i',
                'ó' => 'o',
                'ö' => 'o',
                'ő' => 'o',
                'ú' => 'u',
                'ü' => 'u',
                'ű' => 'u',
                _ => str[i]
            };
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                sb.Append(c);
        }

        return sb.ToString();
    }

    public static string FormatDateRange(DateTime startUtc, DateTime? endUtc)
    {
        DateTime start = TimeZoneInfo.ConvertTimeFromUtc(startUtc, HungarianTimeZone);
        string result = start.ToString("f", HungarianCulture);
        if (!endUtc.HasValue)
            return result;

        DateTime end = TimeZoneInfo.ConvertTimeFromUtc(endUtc.Value, HungarianTimeZone);
        return result + "-" + end.ToString("t", HungarianCulture);
    }

    public static string FormatDateFull(DateTime dateUtc)
    {
        DateTime date = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, HungarianTimeZone);
        StringBuilder sb = new(96);
        sb.Append(date.ToString("D", HungarianCulture));
        sb.Append(' ');
        sb.Append(date.ToString("t", HungarianCulture));
        sb.Append(' ');
        sb.Append('(');
        var zoneId = HungarianTimeZone.IsDaylightSavingTime(dateUtc)
            ? HungarianTimeZone.DaylightName
            : HungarianTimeZone.StandardName;
        sb.Append(zoneId);
        sb.Append(')');
        return sb.ToString();
    }

    public static string FormatDateShort(DateTime dateUtc)
    {
        DateTime nowUtc = DateTime.UtcNow;
        TimeSpan elapsed = nowUtc - dateUtc;
        DateTime date = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, HungarianTimeZone);
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, HungarianTimeZone);

        if (elapsed.TotalMinutes < 59)
            return $"{elapsed.TotalMinutes:0} perce";
        if (elapsed.TotalHours < 10)
            return $"{elapsed.TotalHours:0} órája";
        if (date.Date == now.Date)
            return $"ma, {date:t}";
        if (date.Date == now.Date.AddDays(-1))
            return $"tegnap";
        if (date.Date == now.Date.AddDays(-2))
            return $"tegnapelőtt";
        if (elapsed.TotalDays < 7)
            return $"{elapsed.TotalDays:0} napja";
        if (date.Year == now.Year)
            return date.ToString("MMM dd.", HungarianCulture);
        return date.ToString("yy. MMM dd.", HungarianCulture);
    }

    /// Returns s with at most length characters
    public static string Trim(this string s, int length)
    {
        if (s.Length <= length)
            return s;
        return s[..length];
    }
}
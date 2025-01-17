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

    public static string FormatDate(DateTime dateUtc)
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime date = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, HungarianTimeZone);
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, HungarianTimeZone);

        return dateUtc > nowUtc
            ? FormatFutureDate(dateUtc - nowUtc, date, now)
            : FormatPastDate(nowUtc - dateUtc, date, now);
    }

    private static string FormatPastDate(TimeSpan timeSince, DateTime date, DateTime now)
    {
        if (timeSince.TotalMinutes < 59)
            return $"{timeSince.TotalMinutes:0} perce";
        if (timeSince.TotalHours < 10)
            return $"{timeSince.TotalHours:0} órája";
        if (date.Date == now.Date)
            return "ma, " + date.ToString("t", HungarianCulture);
        if (date.Date == now.Date.AddDays(-1))
            return "tegnap";
        if (date.Date == now.Date.AddDays(-2))
            return "tegnapelőtt";
        if (timeSince.TotalDays < 7)
            return $"{timeSince.TotalDays:0} napja";
        if (date.Year == now.Year)
            return date.ToString("MMM dd.", HungarianCulture);
        return date.ToString("yy. MMM dd.", HungarianCulture);
    }

    private static string FormatFutureDate(TimeSpan timeUntil, DateTime date, DateTime now)
    {
        if (timeUntil.TotalMinutes < 59)
            return $"{timeUntil.TotalMinutes:0} perc múlva";
        if (timeUntil.TotalHours < 10)
            return $"{timeUntil.TotalHours:0} óra múlva";
        if (date.Date == now.Date)
            return "ma, " + date.ToString("t", HungarianCulture);
        if (date.Date == now.Date.AddDays(1))
            return "holnap, " + date.ToString("t", HungarianCulture);
        if (date.Date == now.Date.AddDays(-2))
            return "holnapután, " + date.ToString("t", HungarianCulture);
        if (timeUntil.TotalDays < 7)
            return date.ToString("dddd, HH:mm", HungarianCulture);
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
using System.Text;

namespace StartSch;

/// Takes UTC time, outputs time in Hungary, formatted according to Hungarian rules
//
// https://e-nyelv.hu/2014-05-24/datum-es-idopont/
public static class DateFormatter
{
    public const char EnDash = '–'; // not dash
    
    // tegnapelőtt 19:30
    // tegnap 19:30
    // ma 19:30
    // ma 19:30 (1 óra múlva)
    // ma 19:30 (10 perc múlva)
    // ma 19:30 (most)
    // ma 19:30 (10 perccel ezelőtt)
    // ma 19:30 (1 órával ezelőtt)
    // holnap 00:10 (11 perc múlva)
    // holnap 19:30
    // holnapután 19:30
    // vasárnap 19:30
    // jövő vasárnap 19:30
    // szept. 12., szerda, 19:30
    // 2025. szept. 25., csütörtök, 19:30
    
    // ma 19:30-20:00
    // ma 19:30 (1 óra múlva) – 20:00
    // ma 19:30 (10 perc múlva) - holnap 00:10
    // holnap 19:30 - szept. 12., szerda, 20:00
    // 2025. szept. 25., csütörtök, 19:30-20:00
    // 2025. szept. 25., csütörtök, 19:30 - 26., péntek, 00:10
    // 2025. szept. 25., csütörtök, 19:30 - szept. 27., szombat, 20:00
    // 2025. dec. 31., szerda, 19:30 - 2026. jan. 1., csütörtök, 20:00
    // ma 19:30 - 2026. jan. 1., csütörtök, 20:00
    public static string FormatDateRange(DateTime startUtc, DateTime? endUtc)
    {
        DateTime start = TimeZoneInfo.ConvertTimeFromUtc(startUtc, Utils.HungarianTimeZone);
        string result = start.ToString("f", Utils.HungarianCulture);
        if (!endUtc.HasValue)
            return result;

        DateTime end = TimeZoneInfo.ConvertTimeFromUtc(endUtc.Value, Utils.HungarianTimeZone);
        return result + "-" + end.ToString("t", Utils.HungarianCulture);
    }

    public static string FormatHungarianTime(DateTime date, DateTime? end, DateTime now)
    {
        return "most-most";
    }

    public static string FormatDateFull(DateTime dateUtc)
    {
        DateTime date = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, Utils.HungarianTimeZone);
        StringBuilder sb = new(96);
        sb.Append(date.ToString("D", Utils.HungarianCulture));
        sb.Append(' ');
        sb.Append(date.ToString("t", Utils.HungarianCulture));
        sb.Append(' ');
        sb.Append('(');
        var zoneId = Utils.HungarianTimeZone.IsDaylightSavingTime(dateUtc)
            ? Utils.HungarianTimeZone.DaylightName
            : Utils.HungarianTimeZone.StandardName;
        sb.Append(zoneId);
        sb.Append(')');
        return sb.ToString();
    }

    public static string FormatDate(DateTime dateUtc, DateTime nowUtc)
    {
        DateTime date = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, Utils.HungarianTimeZone);
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Utils.HungarianTimeZone);

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
            return "ma, " + date.ToString("t", Utils.HungarianCulture);
        if (date.Date == now.Date.AddDays(-1))
            return "tegnap";
        if (date.Date == now.Date.AddDays(-2))
            return "tegnapelőtt";
        if (timeSince.TotalDays < 7)
            return $"{timeSince.TotalDays:0} napja";
        if (date.Year == now.Year)
            return date.ToString("MMM dd.", Utils.HungarianCulture);
        return date.ToString("yy. MMM dd.", Utils.HungarianCulture);
    }

    private static string FormatFutureDate(TimeSpan timeUntil, DateTime date, DateTime now)
    {
        if (timeUntil.TotalMinutes < 59)
            return $"{timeUntil.TotalMinutes:0} perc múlva";
        if (timeUntil.TotalHours < 10)
            return $"{timeUntil.TotalHours:0} óra múlva";
        if (date.Date == now.Date)
            return "ma, " + date.ToString("t", Utils.HungarianCulture);
        if (date.Date == now.Date.AddDays(1))
            return "holnap, " + date.ToString("t", Utils.HungarianCulture);
        if (date.Date == now.Date.AddDays(-2))
            return "holnapután, " + date.ToString("t", Utils.HungarianCulture);
        if (timeUntil.TotalDays < 7)
            return date.ToString("dddd, HH:mm", Utils.HungarianCulture);
        if (date.Year == now.Year)
            return date.ToString("MMM dd.", Utils.HungarianCulture);
        return date.ToString("yy. MMM dd.", Utils.HungarianCulture);
    }
}

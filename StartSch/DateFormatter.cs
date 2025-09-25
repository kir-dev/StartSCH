using System.Text;

namespace StartSch;

/// Takes UTC time, outputs time in Hungary, formatted according to Hungarian rules
//
// https://e-nyelv.hu/2014-05-24/datum-es-idopont/
public static class DateFormatter
{
    public const char EnDash = '–'; // not dash

    public static string FormatUtc(DateTime dateUtc, DateTime? endUtc, DateTime nowUtc)
    {
        DateTime dateHu = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, Utils.HungarianTimeZone);
        DateTime? endHu = endUtc.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(endUtc.Value, Utils.HungarianTimeZone) : null;
        DateTime nowHu = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, Utils.HungarianTimeZone);
        return FormatHungarianTime(dateHu, endHu, nowHu);
    }
    
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
    //
    // ma 19:30-20:00
    // ma 19:30 (1 óra múlva) – 20:00
    // ma 19:30 (10 perc múlva) - holnap 00:10
    // holnap 19:30 - szept. 12., szerda, 20:00
    // 2025. szept. 25., csütörtök, 19:30-20:00
    // 2025. szept. 25., csütörtök, 19:30 - 26., péntek, 00:10
    // 2025. szept. 25., csütörtök, 19:30 - szept. 27., szombat, 20:00
    // 2025. dec. 31., szerda, 19:30 - 2026. jan. 1., csütörtök, 20:00
    // ma 19:30 - 2026. jan. 1., csütörtök, 20:00
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
}

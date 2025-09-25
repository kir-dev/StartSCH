using System.Text;
using System.Globalization;

namespace StartSch;

/// Takes UTC time, outputs time in Hungary, formatted according to Hungarian rules
//
// https://e-nyelv.hu/2014-05-24/datum-es-idopont/
public static class DateFormatter
{
    private const char EnDash = '–'; // not dash
    private static readonly string EnDashWithSpaces = $" {EnDash} ";

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
        // date and now are assumed to be in Hungary local time (Europe/Budapest)
        string start = FormatSingle(date, now, includeRelative: true);
        if (end == null)
            return start;

        DateTime endDate = end.Value;
        bool sameDay = date.Date == endDate.Date;

        string endTime = endDate.ToString("HH:mm", Utils.HungarianCulture);

        if (sameDay)
        {
            // Same-day range
            bool hasParen = start.Contains('(');
            if (hasParen)
            {
                // When start contains relative parenthesis, use spaced en-dash
                return start + EnDashWithSpaces + endTime;
            }
            else
            {
                // Compact form without spaces
                return start + EnDash + endTime;
            }
        }

        // Cross-day range: build an appropriate end part label
        string endLabel = FormatEndPart(date, endDate, now);
        return start + EnDashWithSpaces + endLabel;
    }

    private static string FormatSingle(DateTime date, DateTime now, bool includeRelative)
    {
        var sb = new StringBuilder(64);
        // Day label
        string dayLabel = GetDayLabel(date, now);
        sb.Append(dayLabel);
        // If day label is absolute (contains a comma), add a comma before time
        if (dayLabel.Contains(','))
            sb.Append(", ");
        else
            sb.Append(' ');
        sb.Append(date.ToString("HH:mm", Utils.HungarianCulture));

        if (includeRelative)
        {
            string? rel = GetRelativeSuffix(date, now);
            if (rel != null)
            {
                sb.Append(' ');
                sb.Append('(');
                sb.Append(rel);
                sb.Append(')');
            }
        }

        return sb.ToString();
    }

    private static string FormatEndPart(DateTime start, DateTime end, DateTime now)
    {
        // If the end date is near the current date, we can use a relative day label (e.g., "holnap")
        int relDays = (end.Date - now.Date).Days;
        if (relDays is >= -2 and <= 2)
        {
            return GetDayLabel(end, now) + " " + end.ToString("HH:mm", Utils.HungarianCulture);
        }

        // Otherwise, reduce duplication based on sameness with start
        bool sameYear = start.Year == end.Year;
        bool sameMonth = sameYear && start.Month == end.Month;

        var sb = new StringBuilder(64);
        if (!sameYear)
        {
            sb.Append(end.Year);
            sb.Append('.');
            sb.Append(' ');
            sb.Append(GetMonthAbbrev(end.Month));
            sb.Append(' ');
            sb.Append(end.Day);
            sb.Append('.');
        }
        else if (!sameMonth)
        {
            sb.Append(GetMonthAbbrev(end.Month));
            sb.Append(' ');
            sb.Append(end.Day);
            sb.Append('.');
        }
        else
        {
            if (end.Day == start.Day + 1)
            {
                sb.Append(end.Day);
                sb.Append('.');
            }
            else
            {
                sb.Append(GetMonthAbbrev(end.Month));
                sb.Append(' ');
                sb.Append(end.Day);
                sb.Append('.');
            }
        }

        sb.Append(',');
        sb.Append(' ');
        sb.Append(GetWeekdayName(end.DayOfWeek));
        sb.Append(',');
        sb.Append(' ');
        sb.Append(end.ToString("HH:mm", Utils.HungarianCulture));
        return sb.ToString();
    }

    private static string GetDayLabel(DateTime date, DateTime now)
    {
        int diffDays = (date.Date - now.Date).Days;
        return diffDays switch
        {
            -2 => "tegnapelőtt",
            -1 => "tegnap",
            0 => "ma",
            1 => "holnap",
            2 => "holnapután",
            _ => GetLongerDayLabel(date, now)
        };
    }

    private static string GetLongerDayLabel(DateTime date, DateTime now)
    {
        int diffDays = (date.Date - now.Date).Days;
        // Within roughly a week, prefer weekday names
        if (Math.Abs(diffDays) <= 6)
        {
            string wd = GetWeekdayName(date.DayOfWeek);
            if (diffDays > 0)
            {
                // For future days within 6 days, add "jövő " when the target weekday is earlier in the week
                // than today and the difference in days is <= 5 (so Mon→Sun stays "vasárnap", Tue→Sun becomes "jövő vasárnap").
                if (date.DayOfWeek < now.DayOfWeek && diffDays <= 5)
                    return "jövő " + wd;
            }
            return wd;
        }

        // Far away: absolute date; include year only if not current year
        bool sameYearAsNow = date.Year == now.Year;
        var sb = new StringBuilder(64);
        if (!sameYearAsNow)
        {
            sb.Append(date.Year);
            sb.Append('.');
            sb.Append(' ');
        }
        sb.Append(GetMonthAbbrev(date.Month));
        sb.Append(' ');
        sb.Append(date.Day);
        sb.Append('.');
        sb.Append(',');
        sb.Append(' ');
        sb.Append(GetWeekdayName(date.DayOfWeek));
        return sb.ToString();
    }

    private static string? GetRelativeSuffix(DateTime date, DateTime now)
    {
        TimeSpan delta = date - now;
        double minutes = Math.Round(Math.Abs(delta.TotalMinutes));
        if (Math.Abs(delta.TotalMinutes) < 0.5)
            return "most";

        if (Math.Abs(delta.TotalHours) < 1)
        {
            int m = (int)Math.Round(Math.Abs(delta.TotalMinutes));
            if (m == 0) m = 1;
            if (delta.TotalMinutes > 0)
                return m + " perc múlva";
            return m + " perccel ezelőtt";
        }

        if (Math.Abs(delta.TotalHours) < 2)
        {
            if (delta.TotalHours > 0)
                return "1 óra múlva";
            return "1 órával ezelőtt";
        }

        return null;
    }

    private static string GetWeekdayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "hétfő",
            DayOfWeek.Tuesday => "kedd",
            DayOfWeek.Wednesday => "szerda",
            DayOfWeek.Thursday => "csütörtök",
            DayOfWeek.Friday => "péntek",
            DayOfWeek.Saturday => "szombat",
            DayOfWeek.Sunday => "vasárnap",
            _ => ""
        };
    }

    private static string GetMonthAbbrev(int month)
    {
        return month switch
        {
            1 => "jan.",
            2 => "febr.",
            3 => "márc.",
            4 => "ápr.",
            5 => "máj.",
            6 => "jún.",
            7 => "júl.",
            8 => "aug.",
            9 => "szept.",
            10 => "okt.",
            11 => "nov.",
            12 => "dec.",
            _ => ""
        };
    }

    public static string FormatDate(DateTime dateUtc, DateTime nowUtc)
    {
        return FormatUtc(dateUtc, null, nowUtc);
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

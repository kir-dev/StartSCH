using System.Text;

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
        return FormatHungarianTime(dateHu, endHu, nowHu, dateUtc - nowUtc);
    }
    
    public static string FormatHungarianTime(DateTime date, DateTime? end, DateTime now, TimeSpan? timeUntilDate = null)
    {
        DateOnly today = DateOnly.FromDateTime(now);
        DateOnly dateOnly = DateOnly.FromDateTime(date);
        DateFormat dateFormat = GetDateFormat(today, dateOnly);
        timeUntilDate ??= date - now; // this probably breaks around daylight saving time changes, so we calculate it using UTC if possible
        RelativeFormat? relativeFormat = GetRelativeFormat(timeUntilDate.Value);

        var culture = Utils.HungarianCulture;

        StringBuilder sb = new();
        switch (dateFormat)
        {
            case DateFormat.DayBeforeYesterday:
                sb.Append("tegnapelőtt");
                break;
            case DateFormat.Yesterday:
                sb.Append("tegnap");
                break;
            case DateFormat.Today:
                sb.Append("ma");
                break;
            case DateFormat.Tomorrow:
                sb.Append("holnap");
                break;
            case DateFormat.DayAfterTomorrow:
                sb.Append("holnapután");
                break;
            case DateFormat.ThisWeek:
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(date.DayOfWeek));
                break;
            case DateFormat.NextWeek:
                sb.Append("jövő ");
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(date.DayOfWeek));
                break;
            case DateFormat.Month:
                sb.Append(date.ToString("MMM d., dddd,", culture));
                break;
            case DateFormat.Year:
                sb.Append(date.ToString("yyyy. MMM d., dddd,", culture));
                break;
            default:
                throw new();
        }

        sb.Append(' ');
        
        sb.Append(date.ToString("HH:mm", culture));

        var timeSinceDate = timeUntilDate.Value.Negate();
        switch (relativeFormat)
        {
            case RelativeFormat.HoursSince:
                sb.Append(" (");
                sb.Append(timeSinceDate.Hours);
                sb.Append(" órával ezelőtt)");
                break;
            case RelativeFormat.MinutesSince:
                sb.Append(" (");
                sb.Append(timeSinceDate.Minutes);
                sb.Append(" perccel ezelőtt)");
                break;
            case RelativeFormat.Now:
                sb.Append(" (most)");
                break;
            case RelativeFormat.MinutesUntil:
                sb.Append(" (");
                sb.Append(timeUntilDate.Value.Minutes);
                sb.Append(" perc múlva)");
                break;
            case RelativeFormat.HoursUntil:
                sb.Append(" (");
                sb.Append(timeUntilDate.Value.Hours);
                sb.Append(" óra múlva)");
                break;
            case null:
                break;
            default:
                throw new();
        }
        
        if (!end.HasValue)
            return sb.ToString();
        
        DateOnly endDateOnly = DateOnly.FromDateTime(end.Value);
        DateFormat endDateFormat = GetEndDateFormat(dateOnly, endDateOnly, today);

        if (dateOnly == endDateOnly)
        {
            sb.Append(relativeFormat == null ? EnDash : EnDashWithSpaces);
            sb.Append(end.Value.ToString("HH:mm", culture));
            return sb.ToString();
        }
        
        sb.Append(EnDashWithSpaces);
        
        switch (endDateFormat)
        {
            case DateFormat.DayBeforeYesterday:
                sb.Append("tegnapelőtt");
                break;
            case DateFormat.Yesterday:
                sb.Append("tegnap");
                break;
            case DateFormat.Today:
                sb.Append("ma");
                break;
            case DateFormat.Tomorrow:
                sb.Append("holnap");
                break;
            case DateFormat.DayAfterTomorrow:
                sb.Append("holnapután");
                break;
            case DateFormat.ThisWeek:
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(end.Value.DayOfWeek));
                break;
            case DateFormat.NextWeek:
                sb.Append("jövő ");
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(end.Value.DayOfWeek));
                break;
            case DateFormat.Month:
                sb.Append(end.Value.ToString("MMM d., dddd,", culture));
                break;
            case DateFormat.Year:
                sb.Append(end.Value.ToString("yyyy. MMM d., dddd,", culture));
                break;
            default:
                throw new();
        }
        
        sb.Append(' ');
        
        sb.Append(end.Value.ToString("HH:mm", culture));
        
        return sb.ToString();
    }

    private enum DateFormat
    {
        DayBeforeYesterday, // tegnapelőtt
        Yesterday, // tegnap
        Today, // ma
        Tomorrow, // holnap
        DayAfterTomorrow, // holnapután
        ThisWeek, // vasárnap
        NextWeek, // jövő vasárnap
        Month, // szept. 25., csütörtök,
        Year, // 2025. szept. 25., csütörtök,
    }
    
    private enum RelativeFormat
    {
        HoursSince, // (1 órával ezelőtt)
        MinutesSince, // (10 perccel ezelőtt)
        Now, // (most)
        MinutesUntil, // (10 perc múlva)
        HoursUntil, // (1 óra múlva)
    }

    private static DateFormat GetDateFormat(DateOnly today, DateOnly date)
    {
        var daysFromToday = date.DayNumber - today.DayNumber;
        switch (daysFromToday)
        {
            case -2:
                return DateFormat.DayBeforeYesterday;
            case -1:
                return DateFormat.Yesterday;
            case 0:
                return DateFormat.Today;
            case 1:
                return DateFormat.Tomorrow;
            case 2:
                return DateFormat.DayAfterTomorrow;
        }

        var mondayOfDate = GetMondayOfWeekOf(date);
        var mondayOfThisWeek = GetMondayOfWeekOf(today);
        if (mondayOfThisWeek == mondayOfDate)
            return DateFormat.ThisWeek;
        if (mondayOfThisWeek.AddDays(7) == mondayOfDate)
            return DateFormat.NextWeek;
        if (date.Year != today.Year)
            return DateFormat.Year;
        return DateFormat.Month;
    }

    private static DateFormat GetEndDateFormat(DateOnly from, DateOnly to, DateOnly today)
    {
        var daysFromToday = to.DayNumber - today.DayNumber;
        switch (daysFromToday)
        {
            case -2:
                return DateFormat.DayBeforeYesterday;
            case -1:
                return DateFormat.Yesterday;
            case 0:
                return DateFormat.Today;
            case 1:
                return DateFormat.Tomorrow;
            case 2:
                return DateFormat.DayAfterTomorrow;
        }

        var mondayOfDate = GetMondayOfWeekOf(to);
        var mondayOfThisWeek = GetMondayOfWeekOf(today);
        if (mondayOfThisWeek == mondayOfDate)
            return DateFormat.ThisWeek;
        if (mondayOfThisWeek.AddDays(7) == mondayOfDate)
            return DateFormat.NextWeek;
        if (from.Year != to.Year)
            return DateFormat.Year;
        return DateFormat.Month;
    }

    private static RelativeFormat? GetRelativeFormat(TimeSpan timeUntilDate)
    {
        if (timeUntilDate >= TimeSpan.FromHours(2))
            return null;
        if (timeUntilDate >= TimeSpan.FromHours(1))
            return RelativeFormat.HoursUntil;
        if (timeUntilDate >= TimeSpan.FromMinutes(1))
            return RelativeFormat.MinutesUntil;
        var timeSinceDate = timeUntilDate.Negate();
        if (timeSinceDate < TimeSpan.FromMinutes(1))
            return RelativeFormat.Now;
        if (timeSinceDate < TimeSpan.FromHours(1))
            return RelativeFormat.MinutesSince;
        if (timeSinceDate < TimeSpan.FromHours(2))
            return RelativeFormat.HoursSince;
        return null;
    }

    private static DateOnly GetMondayOfWeekOf(DateOnly date)
    {
        DayOfWeek dayOfWeek = date.DayOfWeek;
        int offset = dayOfWeek switch
        {
            DayOfWeek.Sunday => -6,
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => -1,
            DayOfWeek.Wednesday => -2,
            DayOfWeek.Thursday => -3,
            DayOfWeek.Friday => -4,
            DayOfWeek.Saturday => -5,
            _ => throw new()
        };
        return date.AddDays(offset);
    }
}

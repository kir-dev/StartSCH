using System.Text;
using NodaTime.Extensions;

namespace StartSch;

/// Outputs time in Hungary, formatted according to Hungarian rules
//
// https://helyesiras.mta.hu/helyesiras/default/akh12#F11_0_0_2
// https://e-nyelv.hu/2014-05-24/datum-es-idopont/
public static class DateFormatter
{
    private const char EnDash = '–'; // "nagykötőjel"
    private static readonly string EnDashWithSpaces = $" {EnDash} ";

    public static string Format(Instant date, Instant? end, Instant now)
    {
        ZonedDateTime dateHu = date.InZone(Utils.HungarianTimeZone);
        ZonedDateTime? endHu = end?.InZone(Utils.HungarianTimeZone);
        ZonedDateTime nowHu = now.InZone(Utils.HungarianTimeZone);
        return FormatHungarianTime(dateHu, endHu, nowHu, date - now);
    }
    
    public static string FormatHungarianTime(ZonedDateTime date, ZonedDateTime? end, ZonedDateTime now, Duration? timeUntilDate = null)
    {
        LocalDate today = now.Date;
        LocalDate dateOnly = date.Date;
        DateFormat dateFormat = GetDateFormat(today, dateOnly);
        timeUntilDate ??= date - now;
        RelativeFormat? relativeFormat = GetRelativeFormat(timeUntilDate.Value);

        var culture = Utils.HungarianCulture;

        StringBuilder sb = new();
        switch (dateFormat)
        {
            case DateFormat.Yesterday:
                sb.Append("tegnap");
                break;
            case DateFormat.Today:
                sb.Append("ma");
                break;
            case DateFormat.Tomorrow:
                sb.Append("holnap");
                break;
            case DateFormat.ThisWeek:
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(date.DayOfWeek.ToDayOfWeek()));
                break;
            case DateFormat.NextWeek:
                sb.Append("jövő ");
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(date.DayOfWeek.ToDayOfWeek()));
                break;
            case DateFormat.Month:
                sb.Append(date.ToString("MMM d., ddd,", culture));
                break;
            case DateFormat.Year:
                sb.Append(date.ToString("yyyy. MMM d., ddd,", culture));
                break;
            default:
                throw new();
        }

        sb.Append(' ');
        
        sb.Append(date.ToString("HH:mm", culture));

        var timeSinceDate = -timeUntilDate.Value;
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
        
        LocalDate endDateOnly = end.Value.Date;
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
            case DateFormat.Yesterday:
                sb.Append("tegnap");
                break;
            case DateFormat.Today:
                sb.Append("ma");
                break;
            case DateFormat.Tomorrow:
                sb.Append("holnap");
                break;
            case DateFormat.ThisWeek:
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(end.Value.DayOfWeek.ToDayOfWeek()));
                break;
            case DateFormat.NextWeek:
                sb.Append("jövő ");
                sb.Append(Utils.HungarianCulture.DateTimeFormat.GetDayName(end.Value.DayOfWeek.ToDayOfWeek()));
                break;
            case DateFormat.Month:
                sb.Append(end.Value.ToString("MMM d., ddd,", culture));
                break;
            case DateFormat.Year:
                sb.Append(end.Value.ToString("yyyy. MMM d., ddd,", culture));
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
        Yesterday, // tegnap
        Today, // ma
        Tomorrow, // holnap
        ThisWeek, // vasárnap
        NextWeek, // jövő vasárnap
        Month, // szept. 25., cs.,
        Year, // 2025. szept. 25., cs.,
    }
    
    private enum RelativeFormat
    {
        HoursSince, // (1 órával ezelőtt)
        MinutesSince, // (10 perccel ezelőtt)
        Now, // (most)
        MinutesUntil, // (10 perc múlva)
        HoursUntil, // (1 óra múlva)
    }

    private static DateFormat GetDateFormat(LocalDate today, LocalDate date)
    {
        var daysFromToday = (date - today).Days;
        switch (daysFromToday)
        {
            case -1:
                return DateFormat.Yesterday;
            case 0:
                return DateFormat.Today;
            case 1:
                return DateFormat.Tomorrow;
        }

        var mondayOfDate = Utils.GetMondayOfWeekOf(date);
        var mondayOfThisWeek = Utils.GetMondayOfWeekOf(today);
        if (mondayOfThisWeek == mondayOfDate)
            return DateFormat.ThisWeek;
        if (mondayOfThisWeek.PlusDays(7) == mondayOfDate)
            return DateFormat.NextWeek;
        if (date.Year != today.Year)
            return DateFormat.Year;
        return DateFormat.Month;
    }

    private static DateFormat GetEndDateFormat(LocalDate from, LocalDate to, LocalDate today)
    {
        var daysFromToday = (to - today).Days;
        switch (daysFromToday)
        {
            case -1:
                return DateFormat.Yesterday;
            case 0:
                return DateFormat.Today;
            case 1:
                return DateFormat.Tomorrow;
        }

        var mondayOfDate = Utils.GetMondayOfWeekOf(to);
        var mondayOfThisWeek = Utils.GetMondayOfWeekOf(today);
        if (mondayOfThisWeek == mondayOfDate)
            return DateFormat.ThisWeek;
        if (mondayOfThisWeek.PlusDays(7) == mondayOfDate)
            return DateFormat.NextWeek;
        if (from.Year != to.Year)
            return DateFormat.Year;
        return DateFormat.Month;
    }

    private static RelativeFormat? GetRelativeFormat(Duration timeUntilDate)
    {
        if (timeUntilDate >= Duration.FromHours(2))
            return null;
        if (timeUntilDate >= Duration.FromHours(1))
            return RelativeFormat.HoursUntil;
        if (timeUntilDate >= Duration.FromMinutes(1))
            return RelativeFormat.MinutesUntil;
        var timeSinceDate = -timeUntilDate;
        if (timeSinceDate < Duration.FromMinutes(1))
            return RelativeFormat.Now;
        if (timeSinceDate < Duration.FromHours(1))
            return RelativeFormat.MinutesSince;
        if (timeSinceDate < Duration.FromHours(2))
            return RelativeFormat.HoursSince;
        return null;
    }
}

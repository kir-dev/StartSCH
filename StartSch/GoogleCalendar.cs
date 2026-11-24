using System.Text;
using NodaTime.Text;
using StartSch.Data;

namespace StartSch;

/// Builds links to Google Calendar's prefilled "create event" page. No API key or OAuth needed.
public static class GoogleCalendar
{
    private static readonly InstantPattern UtcPattern = InstantPattern.CreateWithInvariantCulture("uuuuMMdd'T'HHmmss'Z'");
    private static readonly LocalDatePattern DatePattern = LocalDatePattern.CreateWithInvariantCulture("uuuuMMdd");
    private static readonly Duration DefaultDuration = Duration.FromHours(1);

    /// Returns null if the event has no start time.
    /// <param name="ev">The event to add to the calendar</param>
    /// <param name="detailsUrl">Absolute URL of the StartSCH page, appended to the description</param>
    /// <param name="textExcerpt">Plain text description excerpt</param>
    public static string? GetAddEventUrl(Event ev, string detailsUrl, string? textExcerpt)
    {
        if (ev.Start is not { } start)
            return null;

        string dates;
        if (ev.AllDay)
        {
            // end date is exclusive, same as in the ICS export
            (LocalDate startDate, LocalDate endDate) = Utils.AllDayGetDates(start, ev.End);
            dates = $"{DatePattern.Format(startDate)}/{DatePattern.Format(endDate)}";
        }
        else
        {
            Instant end = ev.End is { } e && e > start ? e : start + DefaultDuration;
            dates = $"{UtcPattern.Format(start)}/{UtcPattern.Format(end)}";
        }

        string details = string.IsNullOrWhiteSpace(textExcerpt)
            ? detailsUrl
            : $"{textExcerpt.Trim()}\n\n{detailsUrl}";

        StringBuilder sb = new("https://calendar.google.com/calendar/render?action=TEMPLATE");
        sb.Append("&text=").Append(Uri.EscapeDataString(ev.Title));
        sb.Append("&dates=").Append(Uri.EscapeDataString(dates));
        sb.Append("&details=").Append(Uri.EscapeDataString(details));
        return sb.ToString();
    }
}

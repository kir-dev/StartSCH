using System.Runtime.InteropServices;
using NodaTime;

namespace StartSch.Wasm;

public class PersonalCalendarContextDto
{
    public required List<PersonalCalendarLive> Calendars { get; set; }
    public required PersonalCalendarConfigurationDto Configuration { get; set; }
}

public class PersonalCalendarConfigurationDto
{
    public List<Modification> Modifications { get; set; } = [];
}

public class PersonalCalendarContext
{
    private List<PersonalCalendarLive> Calendars { get; }
    private PersonalCalendarConfiguration Configuration { get; }

    private readonly Dictionary<(PersonalCalendarLive, string), PersonalCalendarEvent> _calAndIdToEvent = [];
    private readonly SortedSet<EventIndexEntry> _eventsByStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByEnd = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedEnd = [];
    private readonly Dictionary<NeptunSeriesKey, SortedSet<EventIndexEntry>> _seriesToEvents = [];

    public PersonalCalendarContext(PersonalCalendarContextDto dto)
    {
        Calendars = dto.Calendars;
        Configuration = new(dto.Configuration);
        foreach (PersonalCalendarLive c in Calendars)
        foreach (PersonalCalendarEvent e in c.Events)
        {
            e.CalendarId = c.Id;
            _eventsByStart.Add(new(e.Start, e.Id, e));
            _eventsByEnd.Add(new(e.End, e.Id, e));
            _calAndIdToEvent.Add((c, e.Id), e);

            if (e is { Subject: { } subject, Course: { } course })
            {
                var zonedDateTime = e.Start.InZone(SharedUtils.HungarianTimeZone);
                NeptunSeriesKey key = new(new(subject, course), zonedDateTime.DayOfWeek, zonedDateTime.TimeOfDay);
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_seriesToEvents, key, out _);
                (entry ??= []).Add(new(e.Start, e.Id, e));
            }
        }
    }

    public List<PersonalCalendarEvent> GetEventsIntersectingRange(
        (Instant Start, Instant End) range,
        bool applyModifications)
    {
        EventIndexEntry startKey = new(range.Start.PlusTicks(-1), null!, null!);
        EventIndexEntry endKey = new(range.End.PlusTicks(1), null!, null!);
        return _eventsByStart
            .GetViewBetween(startKey, endKey)
            .Select(x => x.Event)
            .Union(
                _eventsByEnd
                    .GetViewBetween(startKey, endKey)
                    .Select(x => x.Event)
            )
            .ToList();
    }

    public EventEditContext GetEditContext(int calendarId, string eventId)
    {
        var cal = Calendars.First(x => x.Id == calendarId);
        var ev = _calAndIdToEvent[(cal, eventId)];
        var time = ev.Start.InZone(SharedUtils.HungarianTimeZone);
        return new()
        {
            SourceEvent = ev,
            Series = ev is { Subject: { } subject, Course: { } course }
                ? _seriesToEvents[new(new(subject, course), time.DayOfWeek, time.TimeOfDay)]
                    .Select(x => x.Event)
                    .ToList()
                : null,
        };
    }

    private readonly record struct NeptunSeriesKey(
        NeptunSubjectAndCourse SubjectAndCourse,
        IsoDayOfWeek DayOfWeek,
        LocalTime Time
    );

    private readonly record struct EventIndexEntry(
        Instant Instant,
        string Id,
        PersonalCalendarEvent Event
    ) : IComparable<EventIndexEntry>
    {
        public int CompareTo(EventIndexEntry other)
        {
            var instantComparison = Instant.CompareTo(other.Instant);
            return instantComparison != 0
                ? instantComparison
                : string.Compare(Id, other.Id, StringComparison.Ordinal);
        }
    }
}

public class EventEditContext
{
    public PersonalCalendarEvent SourceEvent { get; set; }
    public List<PersonalCalendarEvent>? Series { get; set; }
    public List<Modification> Modifications { get; set; }
    public PersonalCalendarEvent ModifiedEvent { get; set; }
}

public class PersonalCalendarConfiguration
{
    public PersonalCalendarConfiguration(PersonalCalendarConfigurationDto dto)
    {
        foreach (var modification in dto.Modifications)
            NeptunConfiguration.Modifications[modification.SubjectAndCourse] = modification;
    }

    public NeptunConfiguration NeptunConfiguration { get; } = new();

    public PersonalCalendarConfigurationDto ToDto()
    {
        return new()
        {
            Modifications = NeptunConfiguration.Modifications.Values.ToList(),
        };
    }
}

public class NeptunConfiguration
{
    public Dictionary<NeptunSubjectAndCourse, Modification> Modifications { get; } = [];
}

public record struct NeptunSubjectAndCourse(string Subject, string Course);

public class Modification
{
    public required NeptunSubjectAndCourse SubjectAndCourse { get; set; }
    public required List<Instant> Dates { get; set; }
    public int? NewCategoryId { get; set; }
}

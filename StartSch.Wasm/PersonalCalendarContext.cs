using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NodaTime;

namespace StartSch.Wasm;

public class Modification
{
    public IModificationTarget Target { get; set; }
    public IModificationAction Action { get; set; }
}

public interface IModificationTarget;

public class NeptunSeriesTarget : IModificationTarget
{
    public NeptunSubjectAndCourse SubjectAndCourse { get; set; }
    public SortedSet<Instant> Dates { get; set; }
}

public interface IModificationAction
{
    void Apply(PersonalCalendarEvent target);
}

public class CategoryModification : IModificationAction
{
    public required int NewCategoryId { get; init; }

    public void Apply(PersonalCalendarEvent target) => target.CategoryId = NewCategoryId;
}

public class StartModification : IModificationAction
{
    public required Duration Offset { get; init; }

    public void Apply(PersonalCalendarEvent target) => target.Start += Offset;
}

public class PersonalCalendarContext
{
    // source data
    public List<PersonalCalendarLive> Calendars { get; }
    public HashSet<Modification> Modifications { get; }

    // general indexes
    private readonly Dictionary<(PersonalCalendarLive, string), PersonalCalendarEvent> _calAndIdToEvent = [];
    private readonly SortedSet<EventIndexEntry> _eventsByStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByEnd = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedEnd = [];
    private readonly Dictionary<NeptunSeriesKey, SortedSet<EventIndexEntry>> _seriesToEvents = [];

    // modification indexes
    private readonly Dictionary<(NeptunSubjectAndCourse, Instant), HashSet<Modification>>
        _neptunSeriesAndDateToModifications = [];

    private readonly Dictionary<PersonalCalendarEvent, PersonalCalendarEvent> _eventToModifiedEvent = [];

    public PersonalCalendarContext(List<PersonalCalendarLive> calendars, string configJson)
    {
        var config = JsonSerializer.Deserialize<PersonalCalendarConfigurationDto>(
            configJson, SharedUtils.JsonSerializerOptionsWebWithNodaTime)!;
        Modifications = config.Modifications;
        foreach (var modification in Modifications)
        {
            switch (modification.Target)
            {
                case NeptunSeriesTarget neptunSeriesTarget:
                    foreach (var date in neptunSeriesTarget.Dates)
                        _neptunSeriesAndDateToModifications.AddToCollection(
                            (neptunSeriesTarget.SubjectAndCourse, date),
                            modification
                        );
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        
        Calendars = calendars;
        foreach (PersonalCalendarLive c in Calendars)
        {
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
                    _seriesToEvents.AddToCollection(key, new EventIndexEntry(e.Start, e.Id, e));
                }

                var modifiedEvent = CreateModifiedEvent(e);
                _eventsByModifiedStart.Add(new(modifiedEvent.Start, modifiedEvent.Id, modifiedEvent));
                _eventsByModifiedEnd.Add(new(modifiedEvent.End, modifiedEvent.Id, modifiedEvent));
                _eventToModifiedEvent.Add(e, modifiedEvent);
            }
        }
    }

    public PersonalCalendarEvent CreateModifiedEvent(PersonalCalendarEvent e)
    {
        var modifications = GetModifications(e);
        var modifiedEvent = e.Copy();
        foreach (var modification in modifications)
            modification.Action.Apply(modifiedEvent);
        return modifiedEvent;
    }

    public HashSet<Modification> GetModifications(PersonalCalendarEvent e)
    {
        HashSet<Modification> results = [];

        if (e is { Subject: { } subject, Course: { } course })
        {
            if (_neptunSeriesAndDateToModifications.TryGetValue((new(subject, course), e.Start), out var modifications))
                results.UnionWith(modifications);
        }

        return results;
    }

    public List<PersonalCalendarEvent> GetEventsIntersectingRange(
        (Instant Start, Instant End) range,
        bool applyModifications)
    {
        EventIndexEntry startKey = new(range.Start.PlusTicks(-1), null!, null!);
        EventIndexEntry endKey = new(range.End.PlusTicks(1), null!, null!);
        return (applyModifications ? _eventsByModifiedStart : _eventsByStart)
            .GetViewBetween(startKey, endKey)
            .Select(x => x.Event)
            .Union(
                (applyModifications ? _eventsByModifiedEnd : _eventsByEnd)
                .GetViewBetween(startKey, endKey)
                .Select(x => x.Event)
            )
            .ToList();
    }

    public PersonalCalendarLive? GetModifiedCategory(PersonalCalendarEvent e)
    {
        if (e is not { Subject: { } subject, Course: { } course })
            return null;
        var modifications = GetModifications(e);
        if (modifications
                .Select(x => x.Action)
                .OfType<CategoryModification>()
                .FirstOrDefault()
            is not { } categoryModification)
            return null;
        return Calendars.FirstOrDefault(c => c.Id == categoryModification.NewCategoryId);
    }

    public EventEditContext GetEditContext(int calendarId, string eventId)
    {
        var cal = Calendars.First(x => x.Id == calendarId);
        var ev = _calAndIdToEvent[(cal, eventId)];
        var time = ev.Start.InZone(SharedUtils.HungarianTimeZone);

        PersonalCalendarEvent modifiedEvent = new()
        {
            Id = ev.Id,
            CalendarId = ev.CalendarId,
            Title = ev.Title,
            Start = ev.Start,
            End = ev.End,
            Location = ev.Location,
            Subject = ev.Subject,
            Course = ev.Course,
            Teachers = ev.Teachers?.ToList(),
        };
        EventEditContext result = new()
        {
            SourceEvent = ev,
            ModifiedEvent = modifiedEvent,
        };

        if (ev is { Subject: { } subject, Course: { } course })
        {
            result.Series = _seriesToEvents[new(new(subject, course), time.DayOfWeek, time.TimeOfDay)]
                .Select(x => x.Event)
                .ToList();
            if (_neptunSeriesAndDateToModifications.TryGetValue(new(subject, course),
                    out var modifications))
                result.Modifications = modifications;

            var modifiedCategory = GetModifiedCategory(ev);
            if (modifiedCategory != null)
            {
                modifiedEvent.CategoryId = modifiedCategory.Id;
                modifiedEvent.Category = modifiedCategory;
            }
        }

        return result;
    }
    
    public void AddModification(Modification modification)
    {
        modification.Target
    }

    public void DeleteModification(Modification modification)
    {
        
    }

    public void UpdateCategory(EventEditContext eventEditContext, HashSet<Instant> dates, int newCategoryId)
    {
        var newCategory = Calendars.First(x => x.Id == newCategoryId);
        var modifiedEvent = eventEditContext.ModifiedEvent;
        modifiedEvent.CategoryId = newCategoryId;
        modifiedEvent.Category = newCategory;
        NeptunSubjectAndCourse subjectAndCourse = new(modifiedEvent.Subject, modifiedEvent.Course);
        ref var modificationsList = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _neptunSeriesAndDateToModifications, subjectAndCourse, out bool _);
        modificationsList ??= [];

        modificationsList.RemoveAll(modification =>
        {
            modification.Dates.ExceptWith(dates);
            return modification.Dates.Count == 0;
        });

        modificationsList.Add(new()
        {
            Dates = [..dates],
            SubjectAndCourse = subjectAndCourse,
            NewCategoryId = newCategoryId,
        });
    }

    public PersonalCalendarConfigurationDto GetConfigDto() => throw new NotImplementedException();

    [UsedImplicitly]
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
    public required PersonalCalendarEvent SourceEvent { get; set; }
    public List<PersonalCalendarEvent>? Series { get; set; }
    public List<Modification> Modifications { get; set; } = [];
    public required PersonalCalendarEvent ModifiedEvent { get; set; }
}

public readonly record struct NeptunSubjectAndCourse(string Subject, string Course);

public class PersonalCalendarConfigurationDto
{
    public HashSet<Modification> Modifications { get; set; } = [];
}

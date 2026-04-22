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
    public required NeptunSubjectAndCourse SubjectAndCourse { get; set; }
    public required SortedSet<Instant> SelectedDates { get; set; }
}

public interface IModificationAction
{
    void Apply(PersonalCalendarEvent target);
}

public class CategoryModification : IModificationAction
{
    public required int NewCategoryId { get; init; }

    public void Apply(PersonalCalendarEvent target) => target.CategoryCalendarId = NewCategoryId;
}

public class StartModification : IModificationAction
{
    public required Duration Offset { get; init; }

    public void Apply(PersonalCalendarEvent target) => target.Start += Offset;
}

public class EventContext(PersonalCalendarEvent originalEvent, Func<int, PersonalCalendarLive> getCategoryById)
{
    private readonly HashSet<Modification> _modifications = [];

    private PersonalCalendarEvent? _modifiedEvent;

    public PersonalCalendarEvent OriginalEvent { get; } = originalEvent;
    public PersonalCalendarEvent ModifiedEvent => _modifiedEvent ??= CreateModifiedEvent();
    public IReadOnlySet<Modification> Modifications => _modifications;

    private PersonalCalendarEvent CreateModifiedEvent()
    {
        var modifiedEvent = OriginalEvent.Copy();
        foreach (var modification in _modifications)
            modification.Action.Apply(modifiedEvent);
        modifiedEvent.CategoryCalendar = modifiedEvent.CategoryCalendarId is { } categoryCalendarId
            ? getCategoryById(categoryCalendarId)
            : null;
        return modifiedEvent;
    }

    public void AddModification(Modification modification)
    {
        _modifications.Add(modification);
        InvalidateModifiedEvent();
    }

    public void RemoveModification(Modification modification)
    {
        _modifications.Remove(modification);
        InvalidateModifiedEvent();
    }

    private void InvalidateModifiedEvent() => _modifiedEvent = null;
}

public class PersonalCalendarContext
{
    // source data
    private readonly List<PersonalCalendarLive> _calendars;
    private readonly HashSet<Modification> _modifications;

    // indexes
    private readonly Dictionary<(PersonalCalendarLive, string), EventContext> _calAndIdToEvent = [];
    private readonly SortedSet<EventIndexEntry> _eventsByStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByEnd = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedEnd = [];
    private readonly Dictionary<NeptunSeriesKey, SortedSet<EventIndexEntry>> _seriesToEvents = [];
    private readonly Dictionary<(NeptunSubjectAndCourse, Instant), EventContext> _subjectCourseAndDateToEvent = [];

    public PersonalCalendarContext(List<PersonalCalendarLive> calendars, string configJson)
    {
        _calendars = calendars;
        Func<int, PersonalCalendarLive> getCalendarById = id => _calendars.First(x => x.Id == id);
        foreach (PersonalCalendarLive sourceCalendar in _calendars)
        {
            foreach (PersonalCalendarEvent originalEvent in sourceCalendar.Events)
            {
                EventContext eventContext = new(originalEvent, getCalendarById);
                originalEvent.SourceCalendarId = sourceCalendar.Id;
                _eventsByStart.Add(new(originalEvent.Start, originalEvent.Id, eventContext));
                _eventsByEnd.Add(new(originalEvent.End, originalEvent.Id, eventContext));
                _calAndIdToEvent.Add((sourceCalendar, originalEvent.Id), eventContext);

                if (originalEvent is { Subject: { } subject, Course: { } course })
                {
                    var zonedDateTime = originalEvent.Start.InZone(SharedUtils.HungarianTimeZone);
                    NeptunSeriesKey key = new(new(subject, course), zonedDateTime.DayOfWeek, zonedDateTime.TimeOfDay);
                    _seriesToEvents.AddToCollection(key,
                        new EventIndexEntry(originalEvent.Start, originalEvent.Id, eventContext));
                    _subjectCourseAndDateToEvent[new(new(subject, course), originalEvent.Start)] = eventContext;
                }
            }
        }

        var config = JsonSerializer.Deserialize<PersonalCalendarConfigurationDto>(
            configJson, SharedUtils.JsonSerializerOptionsWebWithNodaTime)!;
        _modifications = config.Modifications;
        foreach (var modification in config.Modifications)
        {
            var targetEvents = FindTargetEvents(modification.Target);
            foreach (var eventContext in targetEvents)
            {
                DeindexModifiedEvent(eventContext);
                eventContext.AddModification(modification);
                IndexModifiedEvent(eventContext);
            }
        }
    }

    private void IndexModifiedEvent(EventContext eventContext)
    {
        var modifiedEvent = eventContext.ModifiedEvent;
        _eventsByModifiedStart.Add(new(modifiedEvent.Start, modifiedEvent.Id, eventContext));
        _eventsByModifiedEnd.Add(new(modifiedEvent.End, modifiedEvent.Id, eventContext));
    }

    private void DeindexModifiedEvent(EventContext eventContext)
    {
        var modifiedEvent = eventContext.ModifiedEvent;
        _eventsByModifiedStart.Remove(new(modifiedEvent.Start, modifiedEvent.Id, eventContext));
        _eventsByModifiedEnd.Remove(new(modifiedEvent.End, modifiedEvent.Id, eventContext));
    }

    public List<EventContext> GetEventsIntersectingRange(
        (Instant Start, Instant End) range,
        bool applyModifications)
    {
        EventIndexEntry startKey = new(range.Start.PlusTicks(-1), null!, null!);
        EventIndexEntry endKey = new(range.End.PlusTicks(1), null!, null!);
        return (applyModifications ? _eventsByModifiedStart : _eventsByStart)
            .GetViewBetween(startKey, endKey)
            .Select(x => x.EventContext)
            .Union(
                (applyModifications ? _eventsByModifiedEnd : _eventsByEnd)
                .GetViewBetween(startKey, endKey)
                .Select(x => x.EventContext)
            )
            .ToList();
    }

    public EventEditContext GetEditContext(int calendarId, string eventId)
    {
        var cal = _calendars.First(x => x.Id == calendarId);
        var e = _calAndIdToEvent[(cal, eventId)];
        var time = e.OriginalEvent.Start.InZone(SharedUtils.HungarianTimeZone);

        List<EventContext>? relatedEvents = null;
        if (e.OriginalEvent is { Subject: { } subject, Course: { } course })
        {
            relatedEvents = _seriesToEvents[new(new(subject, course), time.DayOfWeek, time.TimeOfDay)]
                .Select(x => x.EventContext)
                .ToList();
        }

        return new(e, relatedEvents);
    }

    private HashSet<EventContext> FindTargetEvents(IModificationTarget target)
    {
        return target switch
        {
            NeptunSeriesTarget neptunSeriesTarget => neptunSeriesTarget.SelectedDates
                .Select(date => _subjectCourseAndDateToEvent.GetValueOrDefault((neptunSeriesTarget.SubjectAndCourse, date)))
                .Where(x => x != null)
                .Select(x => x!)
                .ToHashSet(),
            _ => throw new NotImplementedException()
        };
    }

    public void AddModification(Modification modification)
    {
        var actionType = modification.Action.GetType();
        switch (modification.Target)
        {
            case NeptunSeriesTarget neptunSeriesTarget:
            {
                // these have the same action type. remove the selected dates from them
                var overlappingModifications = neptunSeriesTarget.SelectedDates
                    .Select(d => _subjectCourseAndDateToEvent.GetValueOrDefault(
                        (neptunSeriesTarget.SubjectAndCourse, d)
                    ))
                    .Where(x => x != null)
                    .SelectMany(x => x!.Modifications)
                    .Where(x => x.Action.GetType() == actionType)
                    .Distinct();
                foreach (var overlappingModification in overlappingModifications)
                {
                    var targetEvents2 = FindTargetEvents(overlappingModification.Target);
                    foreach (var eventContext1 in targetEvents2)
                    {
                        DeindexModifiedEvent(eventContext1);
                        eventContext1.RemoveModification(overlappingModification);
                        IndexModifiedEvent(eventContext1);
                    }

                    var overlappingTarget = (NeptunSeriesTarget)overlappingModification.Target;
                    overlappingTarget.SelectedDates.ExceptWith(neptunSeriesTarget.SelectedDates);
                    if (overlappingTarget.SelectedDates.Count == 0)
                    {
                        _modifications.Remove(overlappingModification);
                        continue;
                    }

                    var targetEvents = FindTargetEvents(overlappingModification.Target);
                    foreach (var eventContext in targetEvents)
                    {
                        DeindexModifiedEvent(eventContext);
                        eventContext.AddModification(overlappingModification);
                        IndexModifiedEvent(eventContext);
                    }
                }
                break;
            }
            default:
                throw new NotImplementedException();
        }

        _modifications.Add(modification);
        var targetEvents1 = FindTargetEvents(modification.Target);
        foreach (var eventContext1 in targetEvents1)
        {
            DeindexModifiedEvent(eventContext1);
            eventContext1.AddModification(modification);
            IndexModifiedEvent(eventContext1);
        }
    }

    public void DeleteModification(Modification modification)
    {
        var targetEvents = FindTargetEvents(modification.Target);
        foreach (var eventContext in targetEvents)
        {
            DeindexModifiedEvent(eventContext);
            eventContext.RemoveModification(modification);
            IndexModifiedEvent(eventContext);
        }

        _modifications.Remove(modification);
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
        EventContext EventContext
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

public record EventEditContext(EventContext EventContext, List<EventContext>? RelatedEvents);

public readonly record struct NeptunSubjectAndCourse(string Subject, string Course);

public class PersonalCalendarConfigurationDto
{
    public HashSet<Modification> Modifications { get; set; } = [];
}

using System.Text.Json;
using JetBrains.Annotations;
using NodaTime;

namespace StartSch.Wasm;

public record Modification(
    IModificationTarget Target,
    IModificationAction Action
);

public interface IModificationTarget
{
    /// <returns>true, if there are no more targets, and the modification can therefore be garbage collected</returns>
    bool RemoveTarget(EventContext eventContext);
}

public class NeptunSeriesTarget : IModificationTarget
{
    public required NeptunSubjectAndCourse SubjectAndCourse { get; set; }
    public required SortedSet<Instant> SelectedDates { get; set; }
    
    public bool RemoveTarget(EventContext eventContext)
    {
        SelectedDates.Remove(eventContext.OriginalEvent.Start);
        return SelectedDates.Count == 0;
    }
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

    public PersonalCalendarEvent OriginalEvent => originalEvent;
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
                eventContext.AddModification(modification);
        }
        
        foreach (var eventIndexEntry in _eventsByStart)
            IndexModifiedEvent(eventIndexEntry.EventContext);
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
        // The modification will apply to these events, overwriting previous modifications of the same action type.
        // Remove the events from overwritten modifications.
        var targetEvents = FindTargetEvents(modification.Target);
        foreach (var targetEvent in targetEvents)
        {
            DeindexModifiedEvent(targetEvent);
            var overwrittenModification = targetEvent.Modifications
                .FirstOrDefault(m => m.Action.GetType() == actionType);
            if (overwrittenModification == null)
                continue;
            bool shouldDeleteOldModification = overwrittenModification.Target.RemoveTarget(targetEvent);
            if (shouldDeleteOldModification)
                _modifications.Remove(overwrittenModification);
            targetEvent.RemoveModification(overwrittenModification);
        }

        _modifications.Add(modification);
        foreach (var eventContext in targetEvents)
        {
            eventContext.AddModification(modification);
            IndexModifiedEvent(eventContext);
        }
    }

    public void RevertModifications(IModificationTarget target, Type actionType)
    {
        var targetEvents = FindTargetEvents(target);
        foreach (var eventContext in targetEvents)
        {
            DeindexModifiedEvent(eventContext);
            var modification = eventContext.Modifications.First(m => m.Action.GetType() == actionType);
            eventContext.RemoveModification(modification);
            if (modification.Target.RemoveTarget(eventContext))
                _modifications.Remove(modification);
            IndexModifiedEvent(eventContext);
        }
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

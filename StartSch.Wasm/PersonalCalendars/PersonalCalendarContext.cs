using System.Text.Json;
using JetBrains.Annotations;

namespace StartSch.Wasm.PersonalCalendars;

public class PersonalCalendarContextDto
{
    public List<PersonalCalendarLive> Calendars { get; set; } = null!;
    public required int DefaultCategoryId { get; set; }
    public required int DefaultExamCategoryId { get; set; }
    public string? ConfigJson { get; set; }
}

public class PersonalCalendarContext
{
    // source data
    private readonly Dictionary<int, PersonalCalendarLive> _calendars;
    private readonly HashSet<Modification> _modifications;
    private PersonalCalendarCategoryLive _defaultCategory;
    private PersonalCalendarCategoryLive _defaultExamCategory;

    // indexes
    private readonly Dictionary<int, HashSet<EventContext>> _calIdToEvents = [];
    private readonly Dictionary<(int, string), EventContext> _calAndIdToEvent = [];
    private readonly SortedSet<EventIndexEntry> _eventsByStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByEnd = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedStart = [];
    private readonly SortedSet<EventIndexEntry> _eventsByModifiedEnd = [];
    private readonly Dictionary<int, HashSet<EventContext>> _eventsByCategoryId = [];
    private readonly HashSet<EventContext> _eventsInDefaultCategory = [];
    private readonly HashSet<EventContext> _eventsInDefaultExamCategory = [];
    private readonly TargetIndex _targetIndex = new();

    private readonly Func<int, PersonalCalendarCategoryLive> _getCategoryById;
    private readonly Func<PersonalCalendarCategoryLive> _getDefaultCategory;
    private readonly Func<PersonalCalendarCategoryLive> _getDefaultExamCategory;

    public PersonalCalendarContext(PersonalCalendarContextDto dto)
    {
        _calendars = dto.Calendars.ToDictionary(x => x.Id);
        _defaultCategory = (PersonalCalendarCategoryLive)_calendars[dto.DefaultCategoryId];
        _defaultExamCategory = (PersonalCalendarCategoryLive)_calendars[dto.DefaultExamCategoryId];
        _getCategoryById = id => (PersonalCalendarCategoryLive)_calendars[id];
        _getDefaultCategory = () => _defaultCategory;
        _getDefaultExamCategory = () => _defaultExamCategory;
        var config = dto.ConfigJson is null
            ? new() { Modifications = [] }
            : JsonSerializer.Deserialize<PersonalCalendarConfigurationDto>(
                dto.ConfigJson, SharedUtils.JsonSerializerOptionsWebWithNodaTime
            )!;
        _modifications = config.Modifications;

        foreach (var calendar in _calendars.Values)
            AddOriginalEvents(calendar, calendar.Events);

        foreach (var modification in config.Modifications)
            foreach (var eventContext in modification.Target.GetTargets(_targetIndex))
                eventContext.AddModification(modification);

        foreach (var eventIndexEntry in _eventsByStart)
            IndexModifiedEvent(eventIndexEntry.EventContext);
    }

    private List<EventContext> AddOriginalEvents(PersonalCalendarLive sourceCalendar, IEnumerable<PersonalCalendarEvent> events)
    {
        List<EventContext> results = [];
        foreach (PersonalCalendarEvent originalEvent in events)
        {
            EventContext eventContext = new(originalEvent, _getCategoryById);
            results.Add(eventContext);
            originalEvent.SourceCalendarId = sourceCalendar.Id;
            originalEvent.GetDefaultCategory = originalEvent switch
            {
                { SpecialType: PersonalCalendarEventSpecialType.Final } => _getDefaultExamCategory,
                _ => _getDefaultCategory,
            };
            _eventsByStart.Add(new(originalEvent.Start, originalEvent.Id, eventContext));
            _eventsByEnd.Add(new(originalEvent.End, originalEvent.Id, eventContext));
            _calIdToEvents.AddToCollection(sourceCalendar.Id, eventContext);
            _calAndIdToEvent.Add((sourceCalendar.Id, originalEvent.Id), eventContext);
            _targetIndex.Add(eventContext);
        }

        return results;
    }

    public void ReplaceOriginalEvents(PersonalCalendarLive sourceCalendar, IEnumerable<PersonalCalendarEvent> events)
    {
        if (_calIdToEvents.TryGetValue(sourceCalendar.Id, out var oldContexts))
            foreach (EventContext oldContext in oldContexts)
            {
                DeindexModifiedEvent(oldContext);
                
                var originalEvent = oldContext.OriginalEvent;
                _eventsByStart.Remove(new(originalEvent.Start, originalEvent.Id, oldContext));
                _eventsByEnd.Remove(new(originalEvent.End, originalEvent.Id, oldContext));
                _calIdToEvents.RemoveFromCollection(sourceCalendar.Id, oldContext);
                _calAndIdToEvent.Remove((sourceCalendar.Id, originalEvent.Id));
                _targetIndex.Remove(oldContext);
            }
        
        var newContexts = AddOriginalEvents(sourceCalendar, events);
        TargetIndex tempIndex = new();
        foreach (var eventContext in newContexts)
            tempIndex.Add(eventContext);
        foreach (var modification in _modifications)
            foreach (var affectedEvent in modification.Target.GetTargets(tempIndex))
                affectedEvent.AddModification(modification);
        foreach (var eventContext in newContexts)
            IndexModifiedEvent(eventContext);
    }

    public void AddCalendar(PersonalCalendarLive calendar)
    {
        _calendars.Add(calendar.Id, calendar);
    }

    public void RemoveCalendar(PersonalCalendarLive calendar)
    {
        ReplaceOriginalEvents(calendar, []);
    }

    private void IndexModifiedEvent(EventContext eventContext)
    {
        var modifiedEvent = eventContext.ModifiedEvent;
        _eventsByModifiedStart.Add(new(modifiedEvent.Start, modifiedEvent.Id, eventContext));
        _eventsByModifiedEnd.Add(new(modifiedEvent.End, modifiedEvent.Id, eventContext));

        if (modifiedEvent.CategoryCalendarId is { } categoryId)
            _eventsByCategoryId.AddToCollection(categoryId, eventContext);
        else if (eventContext.OriginalEvent.SpecialType == PersonalCalendarEventSpecialType.Final)
            _eventsInDefaultExamCategory.Add(eventContext);
        else
            _eventsInDefaultCategory.Add(eventContext);
    }

    private void DeindexModifiedEvent(EventContext eventContext)
    {
        var modifiedEvent = eventContext.ModifiedEvent;
        _eventsByModifiedStart.Remove(new(modifiedEvent.Start, modifiedEvent.Id, eventContext));
        _eventsByModifiedEnd.Remove(new(modifiedEvent.End, modifiedEvent.Id, eventContext));

        if (modifiedEvent.CategoryCalendarId is { } categoryId)
            _eventsByCategoryId.RemoveFromCollection(categoryId, eventContext);
        else if (eventContext.OriginalEvent.SpecialType == PersonalCalendarEventSpecialType.Final)
            _eventsInDefaultExamCategory.Remove(eventContext);
        else
            _eventsInDefaultCategory.Remove(eventContext);
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
        var cal = _calendars[calendarId];
        var e = _calAndIdToEvent[(cal.Id, eventId)];
        var time = e.OriginalEvent.Start.InZone(SharedUtils.HungarianTimeZone);

        List<EventContext>? relatedEvents = null;
        if (e.OriginalEvent is { Subject: { } subject, Course: { } course })
        {
            relatedEvents = _targetIndex.SeriesToEvents[new(new(subject, course), time.DayOfWeek, time.TimeOfDay)]
                .Select(x => x.EventContext)
                .ToList();
        }

        return new(e, relatedEvents);
    }

    public void AddModification(Modification modification)
    {
        var actionType = modification.Action.GetType();
        // The modification will apply to these events, overwriting previous modifications of the same action type.
        // Remove the events from overwritten modifications.
        var targetEvents = modification.Target.GetTargets(_targetIndex);
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
        var targetEvents = target.GetTargets(_targetIndex);
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

    public HashSet<EventContext> GetEventsInCategory(int categoryId)
    {
        HashSet<EventContext> result = [];
        if (_eventsByCategoryId.TryGetValue(categoryId, out var eventsInCategory))
            result.UnionWith(eventsInCategory);
        if (categoryId == _defaultCategory.Id)
            result.UnionWith(_eventsInDefaultCategory);
        if (categoryId == _defaultExamCategory.Id)
            result.UnionWith(_eventsInDefaultExamCategory);
        return result;
    }

    public PersonalCalendarConfigurationDto GetConfigurationDto() => new() { Modifications = _modifications };
}

[UsedImplicitly]
public readonly record struct NeptunSeriesKey(
    NeptunSubjectAndCourse SubjectAndCourse,
    IsoDayOfWeek DayOfWeek,
    LocalTime Time
);

public readonly record struct EventIndexEntry(
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

public record EventEditContext(EventContext EventContext, List<EventContext>? RelatedEvents);

public readonly record struct NeptunSubjectAndCourse(string Subject, string Course);

public class PersonalCalendarConfigurationDto
{
    public required HashSet<Modification> Modifications { get; set; }
}

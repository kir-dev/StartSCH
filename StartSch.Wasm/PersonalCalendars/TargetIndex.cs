namespace StartSch.Wasm.PersonalCalendars;

/// Indexes original events. Used by IModificationTargets to find their targets.
public class TargetIndex
{
    private readonly Dictionary<(int, string), EventContext> _calAndIdToEvent = [];
    private readonly Dictionary<NeptunSeriesKey, SortedSet<EventIndexEntry>> _seriesToEvents = [];
    private readonly Dictionary<(NeptunSubjectAndCourse, Instant), EventContext> _subjectCourseAndDateToEvent = [];
    
    public IReadOnlyDictionary<(int, string), EventContext> CalAndIdToEvent => _calAndIdToEvent;
    public IReadOnlyDictionary<NeptunSeriesKey, SortedSet<EventIndexEntry>> SeriesToEvents => _seriesToEvents;
    public IReadOnlyDictionary<(NeptunSubjectAndCourse, Instant), EventContext> SubjectCourseAndDateToEvent => _subjectCourseAndDateToEvent;

    public void Add(EventContext eventContext)
    {
        var originalEvent = eventContext.OriginalEvent;
        _calAndIdToEvent.Add((originalEvent.SourceCalendar.Id, originalEvent.Id), eventContext);
        
        if (originalEvent is { Subject: { } subject, Course: { } course })
        {
            var zonedDateTime = originalEvent.Start.InZone(SharedUtils.HungarianTimeZone);
            _seriesToEvents.AddToCollection(
                new(new(subject, course), zonedDateTime.DayOfWeek, zonedDateTime.TimeOfDay),
                new EventIndexEntry(originalEvent.Start, originalEvent.Id, eventContext)
            );
            _subjectCourseAndDateToEvent[new(new(subject, course), originalEvent.Start)] = eventContext;
        }
    }

    public void Remove(EventContext eventContext)
    {
        var originalEvent = eventContext.OriginalEvent;
        _calAndIdToEvent.Remove((originalEvent.SourceCalendar.Id, originalEvent.Id));
        
        if (originalEvent is { Subject: { } subject, Course: { } course })
        {
            var zonedDateTime = originalEvent.Start.InZone(SharedUtils.HungarianTimeZone);
            _seriesToEvents.RemoveFromCollection(
                new(new(subject, course), zonedDateTime.DayOfWeek, zonedDateTime.TimeOfDay),
                new EventIndexEntry(originalEvent.Start, originalEvent.Id, eventContext)
            );
            _subjectCourseAndDateToEvent.Remove(new(new(subject, course), originalEvent.Start));
        }
    }
}

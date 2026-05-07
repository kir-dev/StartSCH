using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace StartSch.Wasm.PersonalCalendars;

[JsonDerivedType(typeof(CalendarAndEventIdTarget), nameof(CalendarAndEventIdTarget))]
[JsonDerivedType(typeof(NeptunSeriesTarget), nameof(NeptunSeriesTarget))]
public interface IModificationTarget
{
    IReadOnlySet<EventContext> GetTargets(TargetIndex index);

    /// Updates the target, such that the given event is no longer matched
    /// <returns>true, if there are no more targets, and the modification can therefore be garbage collected</returns>
    bool RemoveTarget(EventContext eventContext);
}

public class CalendarAndEventIdTarget : IModificationTarget
{
    public required int CalendarId { get; set; }
    public required string EventId { get; set; }

    public IReadOnlySet<EventContext> GetTargets(TargetIndex index) =>
        index.CalAndIdToEvent.TryGetValue((CalendarId, EventId), out var eventContext)
            ? ImmutableHashSet.Create(eventContext)
            : ImmutableHashSet<EventContext>.Empty;

    public bool RemoveTarget(EventContext eventContext) => true;
}

public class NeptunSeriesTarget : IModificationTarget
{
    public required NeptunSubjectAndCourse SubjectAndCourse { get; set; }
    public required SortedSet<Instant> SelectedDates { get; set; }

    public IReadOnlySet<EventContext> GetTargets(TargetIndex index) =>
        SelectedDates
            .Select(date =>
                index.SubjectCourseAndDateToEvent.GetValueOrDefault((SubjectAndCourse, date)))
            .Where(x => x != null)
            .Select(x => x!)
            .ToImmutableHashSet();

    public bool RemoveTarget(EventContext eventContext)
    {
        SelectedDates.Remove(eventContext.OriginalEvent.Start);
        return SelectedDates.Count == 0;
    }
}

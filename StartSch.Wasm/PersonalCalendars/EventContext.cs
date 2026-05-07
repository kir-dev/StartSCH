namespace StartSch.Wasm.PersonalCalendars;

/// stores an event, modifications, and computes the modified event
public class EventContext(
    PersonalCalendarEvent originalEvent,
    Func<int, PersonalCalendarCategoryLive> getCategoryById)
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

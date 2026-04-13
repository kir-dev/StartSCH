using JetBrains.Annotations;

namespace StartSch.Wasm;

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)] 
public record struct FullCalendarEvent(
    string Id,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Title,
    string BackgroundColor,
    string TextColor,
    int CalendarId
)
{
    public FullCalendarExtendedProps ExtendedProps { get; } = new(CalendarId);
}

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)] 
public record struct FullCalendarExtendedProps(int CalendarId);

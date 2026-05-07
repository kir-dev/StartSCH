using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;

namespace StartSch.Wasm;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling#custom-event-arguments
[EventHandler(
    "oncalendarrangechanged",
    typeof(CalendarRangeChangedEventArgs),
    enableStopPropagation: true,
    enablePreventDefault: true
)]
[EventHandler(
    "oncalendareventclicked",
    typeof(CalendarEventClickedEventArgs),
    enableStopPropagation: true,
    enablePreventDefault: true
)]
public static class EventHandlers;

public class CalendarRangeChangedEventArgs : EventArgs
{
    public DateTimeOffset Start { get; [UsedImplicitly] set; }
    public DateTimeOffset End { get; [UsedImplicitly] set; }
}

public class CalendarEventClickedEventArgs : EventArgs
{
    public int CalendarId { get; [UsedImplicitly] set; }
    public string EventId { get; [UsedImplicitly] set; } = null!;
}

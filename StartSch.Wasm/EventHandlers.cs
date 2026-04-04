using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;

namespace StartSch.Wasm;

// https://learn.microsoft.com/en-us/aspnet/core/blazor/components/event-handling#custom-event-arguments
[EventHandler(
    "onfullcalendargetevents",
    typeof(FullCalendarGetEventsEventArgs),
    enableStopPropagation: true,
    enablePreventDefault: true
)]
public static class EventHandlers;

public class FullCalendarGetEventsEventArgs : EventArgs
{
    [JsonPropertyName("startStr")]
    public DateTimeOffset Start { get; set; }
    
    [JsonPropertyName("endStr")]
    public DateTimeOffset End { get; set; }
}

using JetBrains.Annotations;

namespace StartSch.Wasm;

[UsedImplicitly] 
public record FullCalendarEvent(
    string Id,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Title,
    string BackgroundColor,
    string TextColor
);

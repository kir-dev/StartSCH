using JetBrains.Annotations;

namespace StartSch.Wasm;

[UsedImplicitly] 
public record struct FullCalendarEvent(
    string Id,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Title,
    string BackgroundColor,
    string TextColor
);

namespace StartSch.Wasm.PersonalCalendars;

public record Modification(
    IModificationTarget Target,
    IModificationAction Action
);

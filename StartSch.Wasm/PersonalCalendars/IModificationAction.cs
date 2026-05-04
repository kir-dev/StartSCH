using System.Text.Json.Serialization;

namespace StartSch.Wasm.PersonalCalendars;

[JsonDerivedType(typeof(CategoryModification), nameof(CategoryModification))]
[JsonDerivedType(typeof(StartModification), nameof(StartModification))]
public interface IModificationAction
{
    void Apply(PersonalCalendarEvent target);
}

public class CategoryModification : IModificationAction
{
    public required int NewCategoryId { get; init; }

    public void Apply(PersonalCalendarEvent target) => target.CategoryCalendarId = NewCategoryId;
}

public class StartModification : IModificationAction
{
    public required Duration Offset { get; init; }

    public void Apply(PersonalCalendarEvent target) => target.Start += Offset;
}


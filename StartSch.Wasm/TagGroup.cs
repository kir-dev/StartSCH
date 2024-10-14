namespace StartSch.Wasm;

public class TagGroup(
    string id,
    string? description = null,
    List<TagGroup>? children = null)
    : SelectableGroup<TagGroup>(id, children)
{
    public string? Description { get; } = description;
}
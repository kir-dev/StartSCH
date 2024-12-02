using StartSch.Wasm;

namespace StartSch.Services;

public class TagService(IEnumerable<IModule> modules)
{
    public async Task<List<TagGroup>> GetTags()
    {
        var tags = await Task.WhenAll(modules.Select(m => m.GetTags()));
        return TagGroup.Merge(tags.SelectMany(t => t));
    }

    public async Task<List<string>> GetValidTags(IEnumerable<string> tags)
    {
        List<TagGroup> tagGroups = await GetTags();
        TagGroup.DeserializeSelection(tagGroups, tags);
        return tagGroups
            .SelectMany(t => t.SerializeSelection())
            .ToList();
    }
}
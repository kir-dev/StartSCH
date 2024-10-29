using StartSch.Wasm;

namespace StartSch;

public interface IModule
{
    string Id { get; }
    IEnumerable<Post> Posts { get; }
    IEnumerable<Event> Events { get; }
    IEnumerable<Instance> Instances { get; }
    IEnumerable<TagGroup> Tags { get; }
    IEnumerable<Func<CancellationToken, Task<DateTimeOffset>>> CronJobs { get; }
}

public static class ModuleListExtensions
{
    /// <returns>
    /// Every tag from <paramref name="tags"/> where the tag has been published by any of the <paramref name="modules"/>.
    /// The items are guaranteed to be unique.
    /// </returns>
    public static List<string> FilterValidTags(this IEnumerable<IModule> modules, IEnumerable<string> tags)
    {
        List<TagGroup> tagGroups = modules.GetTagGroups();
        TagGroup.DeserializeSelection(tagGroups, tags);
        return tagGroups
            .SelectMany(t => t.SerializeSelection())
            .ToList();
    }

    private static List<TagGroup> GetTagGroups(this IEnumerable<IModule> modules)
    {
        return TagGroup.Merge(modules.SelectMany(m => m.Tags));
    }
}
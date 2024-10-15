using StartSch.Wasm;

namespace StartSch;

public interface IModule
{
    public string Id { get; }
    public IEnumerable<Post> Posts { get; }
    public IEnumerable<Event> Events { get; }
    public IEnumerable<Instance> Instances { get; }
    public IEnumerable<TagGroup> Tags { get; }
}
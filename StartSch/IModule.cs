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
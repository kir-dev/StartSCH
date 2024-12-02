using StartSch.Wasm;

namespace StartSch.Modules.Cmsch;

public class CmschModule : IModule
{
    public string Id => "cmsch";

    private static readonly Task<IEnumerable<Instance>> Instances = Task.FromResult<IEnumerable<Instance>>(
    [
        new("https://qpa.sch.bme.hu", "QPA")
    ]);
    public Task<IEnumerable<Instance>> GetInstances() => Instances;

    private static readonly Task<IEnumerable<TagGroup>> Tags = Task.FromResult<IEnumerable<TagGroup>>(
    [
    ]);
    public Task<IEnumerable<TagGroup>> GetTags() => Tags;
}
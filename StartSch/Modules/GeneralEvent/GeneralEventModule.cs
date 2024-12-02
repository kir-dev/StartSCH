using StartSch.Wasm;

namespace StartSch.Modules.GeneralEvent;

public class GeneralEventModule : IModule
{
    public string Id => "general";

    private static readonly Task<IEnumerable<Instance>> Instances = Task.FromResult<IEnumerable<Instance>>(
    [
        new("https://progkong.sch.bme.hu", "Programozói Konferencia"),
    ]);
    public Task<IEnumerable<Instance>> GetInstances() => Instances;

    private static readonly Task<IEnumerable<TagGroup>> Tags = Task.FromResult<IEnumerable<TagGroup>>(
    [
    ]);
    public Task<IEnumerable<TagGroup>> GetTags() => Tags;
}
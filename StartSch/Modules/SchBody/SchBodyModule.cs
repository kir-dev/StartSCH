using StartSch.Services;
using StartSch.Wasm;

namespace StartSch.Modules.SchBody;

public class SchBodyModule : IModule
{
    private readonly Task<IEnumerable<Instance>> _instances = Task.FromResult<IEnumerable<Instance>>([
        new("https://body.sch.bme.hu", "")
    ]);

    private readonly Task<IEnumerable<TagGroup>> _tags = Task.FromResult<IEnumerable<TagGroup>>([
        new("email", null, [
            new("schbody", "SCHBody", [
                new("hírek", "Email a SCHBody posztjairól"),
            ]),
        ]),
        new("pust", null, [
            new("schbody", "SCHBody", [
                new("hírek", "Push értesítés a SCHBody posztjairól"),
            ]),
        ]),
    ]);

    public string Id => "schbody";

    public Task<IEnumerable<Instance>> GetInstances() => _instances;
    public Task<IEnumerable<TagGroup>> GetTags() => _tags;

    public static void Register(IServiceCollection services)
    {
        services.AddScoped<SchBodyPollJob>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<SchBodyPollJob>()
            .SetInterval(TimeSpan.FromHours(5));
    }
}
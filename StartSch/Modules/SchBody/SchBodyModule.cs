using StartSch.Services;
using StartSch.Wasm;

namespace StartSch.Modules.SchBody;

public class SchBodyModule : IModule
{
    public string Id => "schbody";

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

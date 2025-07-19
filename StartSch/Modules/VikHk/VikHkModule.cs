using StartSch.Services;

namespace StartSch.Modules.VikHk;

public class VikHkModule : IModule
{
    public const int PekId = 68;
    
    public static void Register(IServiceCollection services)
    {
        services.AddScoped<VikHkPollJob>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<VikHkPollJob>()
            .SetInterval(TimeSpan.FromHours(1));
    }
}

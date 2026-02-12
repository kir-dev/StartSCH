using StartSch.Services;
using StartSch.Wasm;

namespace StartSch.Modules.SchBody;

public class SchBodyModule : IModule
{
    public const int PekId = 37;
    public const string Url = "https://body.sch.bme.hu";
    public const string Api = "https://api.body.kir-dev.hu";
    public const string Name = "SCHBody";

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

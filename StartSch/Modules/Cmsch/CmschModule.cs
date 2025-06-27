using StartSch.Services;

namespace StartSch.Modules.Cmsch;

public class CmschModule : IModule
{
    public string Id => "cmsch";

    public List<string> Instances { get; } =
    [
        "https://nyari.sch.bme.hu",
        "https://seniortabor.sch.bme.hu",
    ];

    static void IModule.Register(IServiceCollection services)
    {
        services.AddScoped<CmschPollJob>();
    }

    void IModule.RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<CmschPollJob>().SetInterval(TimeSpan.FromMinutes(10));
    }
}

public class CmschInstance(
    string FrontendUrl,
    DateTime Latest // if the latest event was created before this, start a new event. update to the current date
);

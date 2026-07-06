using StartSch.Services;

namespace StartSch.Modules.VikBmeHu;

public class VikBmeHuModule : IModule
{
    public static string Id => nameof(VikBmeHuModule);

    public const string Url = "https://vik.bme.hu";

    public static void Register(IServiceCollection services)
    {
        services.AddScoped<VikBmeHuPollJob>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService
            .Register<VikBmeHuPollJob>()
            .SetInterval(TimeSpan.FromHours(2));
    }
}

using StartSch.Services;

namespace StartSch.Modules.KthBmeHu;

public class KthBmeHuModule() : IModule
{
    public const string Url = "https://www.kth.bme.hu";
    public const string RssUrl = Url + "/rss";
    public const string CurrentPostUrlPrefix = Url + "/hirek/aktualis/";
    
    public static void Register(IServiceCollection services)
    {
        services.AddScoped<KthBmeHuPollJob>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService
            .Register<KthBmeHuPollJob>()
            .SetInterval(TimeSpan.FromHours(2));
    }
}

using StartSch.Services;

namespace StartSch.Modules.KthBmeHu;

public class KthBmeHuModule() : IModule
{
    public const string Url = "https://www.kth.bme.hu";
    public const string RssUrl = Url + "/rss";
    public const string CurrentPostUrlPrefix = Url + "/hirek/aktualis/";
    public const string CurrentEventUrlPrefix = Url + "/hallgatoknak/idopontok/";
    public const string CalendarEndpoint = Url + "/calendar-ajax/";

    public static string GetEventUrl(int externalId) => CurrentEventUrlPrefix + externalId;
    
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

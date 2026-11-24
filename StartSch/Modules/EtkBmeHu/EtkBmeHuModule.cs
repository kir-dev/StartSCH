using StartSch.Services;

namespace StartSch.Modules.EtkBmeHu;

public class EtkBmeHuModule : IModule
{
    public static string Id => nameof(EtkBmeHuModule);

    public const string Url = "https://etk.bme.hu";
    public const string RssUrl = Url + "/rss";
    public const string CurrentPostUrlPrefix = Url + "/hirek/aktualis/";
    public const string CurrentEventUrlPrefix = Url + "/hallgatoknak/idopontok/";
    public const string CalendarEndpoint = Url + "/calendar-ajax/";

    public static void Register(IServiceCollection services)
    {
        services.AddScoped<EtkBmeHuPollJob>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService
            .Register<EtkBmeHuPollJob>()
            .SetInterval(TimeSpan.FromHours(2));
    }
}

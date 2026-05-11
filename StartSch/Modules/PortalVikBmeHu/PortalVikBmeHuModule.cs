using StartSch.Services;

namespace StartSch.Modules.PortalVikBmeHu;

public class PortalVikBmeHuModule(HttpClient httpClient) : IModule, IPollJobExecutor
{
    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<PortalVikBmeHuModule>()
            .SetInterval(TimeSpan.FromDays(1));
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        httpClient.GetStreamAsync("https://portal.vik.bme.hu/kepzes/targyak/");
    }
}

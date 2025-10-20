using StartSch.Services;

namespace StartSch.Modules.Cmsch;

public class CmschModule : IModule
{
    private readonly List<string> _instances =
    [
        "https://cmsch-karacsony.vercel.app",
        
        "https://cst.sch.bme.hu",
        "https://felezo.sch.bme.hu",
        "https://g7.sch.bme.hu",
        "https://golya.sch.bme.hu",
        "https://golyabal.sch.bme.hu",
        "https://golyakorte.sch.bme.hu",
        "https://kepzes.sch.bme.hu",
        "https://kozelok.sch.bme.hu",
        "https://kozelokepzes.sch.bme.hu",
        "https://nyari.sch.bme.hu",
        "https://qpa.sch.bme.hu",
        "https://seniortabor.sch.bme.hu",
        
        "https://skktv.simonyi.bme.hu",
        "https://snyt.simonyi.bme.hu",
        "https://tanfolyam.simonyi.bme.hu",
        "https://vik75.sch.bme.hu",
        
        "https://meetup.svk.bme.hu",
    ];

    static void IModule.Register(IServiceCollection services)
    {
        services.AddScoped<CmschPollJob>();
    }

    void IModule.RegisterPollJobs(PollJobService pollJobService)
    {
        foreach (string frontendUrl in _instances)
            pollJobService
                .Register<CmschPollJob, string>(frontendUrl)
                .SetInterval(TimeSpan.FromHours(.5));
    }
}

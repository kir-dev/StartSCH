using StartSch.Services;

namespace StartSch.Modules.SchPincer;

public class SchPincerModule : IModule
{
    public const string Url = "https://schpincer.sch.bme.hu";

    public int DefaultCategoryId { get; set; }

    public static void Register(IServiceCollection services)
    {
        services.AddScoped<SchPincerPollJob>();
        services.RegisterModuleInitializer<SchPincerInitializer>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<SchPincerPollJob>()
            .SetInterval(TimeSpan.FromMinutes(10));
    }
}

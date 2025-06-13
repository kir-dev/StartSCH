using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Services;
using StartSch.Wasm;

namespace StartSch.Modules.SchPincer;

public class SchPincerModule(IDbContextFactory<Db> dbFactory, IMemoryCache cache) : IModule
{
    public const string PincerPagesCacheKey = "PincerPages";
    public const string Url = "https://schpincer.sch.bme.hu/";

    public string Id => "pincer";
    
    public async Task<List<Page>> GetPages()
    {
        return (await cache.GetOrCreateAsync(PincerPagesCacheKey, async _ =>
        {
            await using Db db = await dbFactory.CreateDbContextAsync();
            return await db.Pages
                .AsNoTrackingWithIdentityResolution()
                .Include(p => p.Categories)
                .Where(g => g.PincerName != null)
                .ToListAsync();
        }))!;
    }

    public static void Register(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, AdminRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, PageAdminHandler>();
        services.AddScoped<SchPincerPollJob>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<SchPincerPollJob>()
            .SetInterval(TimeSpan.FromMinutes(10));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Services;
using StartSch.Wasm;

namespace StartSch.Modules.SchPincer;

public class SchPincerModule(IDbContextFactory<Db> dbFactory) : IModule
{
    private readonly Task<IEnumerable<Instance>> _instances = Task.FromResult<IEnumerable<Instance>>([
        new("https://schpincer.sch.bme.hu", "SCH-Pincér")
    ]);

    public string Id => "pincer";
    public Task<IEnumerable<Instance>> GetInstances() => _instances;

    public async Task<IEnumerable<TagGroup>> GetTags()
    {
        await using Db db = await dbFactory.CreateDbContextAsync();
        List<string> groups = await db.Groups
            .AsNoTracking()
            .Where(g => g.PincerName != null)
            .Select(g => g.PincerName!)
            .ToListAsync();
        return
        [
            new("nyitások", "Nyitások megjelenítése a főoldalon", [
                ..groups.Select(g => new TagGroup(g))
            ]),
            new("push", null, [
                new("pincér", "SCH-Pincér", [
                    new("hírek", "Push értesítés a körök posztjairól", [
                        ..groups.Select(g => new TagGroup(g))
                    ]),
                    new("rendelés", "Push értesítés rendelés kezdetekor", [
                        ..groups.Select(g => new TagGroup(g))
                    ]),
                    new("nyitás", "Push értesítés nyitás kezdetekor", [
                        ..groups.Select(g => new TagGroup(g))
                    ]),
                ])
            ]),
            new("email", null, [
                new("pincér", "SCH-Pincér", [
                    new("hírek", "Email a körök posztjairól", [
                        ..groups.Select(g => new TagGroup(g))
                    ]),
                    new("rendelés", "Email rendelés kezdetekor", [
                        ..groups.Select(g => new TagGroup(g))
                    ]),
                    new("nyitás", "Email nyitás kezdetekor", [
                        ..groups.Select(g => new TagGroup(g))
                    ]),
                ])
            ]),
        ];
    }

    public async Task<List<Group>> GetGroups()
    {
        await using Db db = await dbFactory.CreateDbContextAsync();
        return await db.Groups
            .AsNoTracking()
            .Where(g => g.PincerName != null)
            .ToListAsync();
    }

    public static void Register(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, AdminRequirementHandler>();
        services.AddScoped<IAuthorizationHandler, PincerGroupAdminRequirementHandler>();
        services.AddScoped<SchPincerPollJob>();
    }

    public void RegisterPollJobs(PollJobService pollJobService)
    {
        pollJobService.Register<SchPincerPollJob>()
            .SetInterval(TimeSpan.FromMinutes(5));
    }
}
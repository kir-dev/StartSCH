using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StartSch.Services;

namespace StartSch;

public interface IModule
{
    static virtual string Id => throw new NotImplementedException();

    static virtual void Register(IServiceCollection services)
    {
    }

    void RegisterPollJobs(PollJobService pollJobService)
    {
    }
}

public static class ModuleExtensions
{
    public static void AddModule<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
            TService>
        (this WebApplicationBuilder webApplicationBuilder) where TService : class, IModule
    {
        var enabledModules = webApplicationBuilder.Configuration.GetSection("StartSch:EnabledModules");
        var enabled = enabledModules["All"] is "True" or "true"
                      || enabledModules[TService.Id] is "True" or "true";
        if (!enabled)
            return;

        IServiceCollection serviceCollection = webApplicationBuilder.Services;
        serviceCollection.AddSingleton<TService>();
        serviceCollection.AddSingleton<IModule, TService>(s => s.GetRequiredService<TService>());
        TService.Register(serviceCollection);
    }
}

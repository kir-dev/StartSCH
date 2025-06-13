using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using StartSch.Services;
using StartSch.Wasm;

namespace StartSch;

public interface IModule
{
    string Id { get; }

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
        (this IServiceCollection serviceCollection) where TService : class, IModule
    {
        serviceCollection.AddSingleton<TService>();
        serviceCollection.AddSingleton<IModule, TService>(s => s.GetRequiredService<TService>());
        TService.Register(serviceCollection);
    }
}

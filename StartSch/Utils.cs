using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace StartSch;

public static class Utils
{
    public static void AddModule<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors),
         MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
        TService>
        (this IServiceCollection serviceCollection) where TService : class, IModule
    {
        serviceCollection.AddSingleton<TService>();
        serviceCollection.AddSingleton<IModule, TService>(s => s.GetRequiredService<TService>());
    }
}
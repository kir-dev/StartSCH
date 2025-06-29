using JetBrains.Annotations;

namespace StartSch;

public interface IModuleInitializer
{
    Task Initialize();
}

public interface IModuleInitializerMarker
{
    Type Type { get; }
}

public class ModuleInitializerMarker<TModuleInitializer>()
    : IModuleInitializerMarker
    where TModuleInitializer : IModuleInitializer
{
    public Type Type => typeof(TModuleInitializer);
}

public static class ModuleInitializerExtensions
{
    public static void RegisterModuleInitializer<
            [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
            TModuleInitializer
        >
        (this IServiceCollection serviceCollection)
        where TModuleInitializer : class, IModuleInitializer
    {
        serviceCollection.AddScoped<TModuleInitializer>();
        serviceCollection.AddSingleton<IModuleInitializerMarker, ModuleInitializerMarker<TModuleInitializer>>();
    }
}

namespace StartSch.Services;

public class ModuleInitializationService(IServiceProvider serviceProvider, IEnumerable<IModuleInitializerMarker> markers)
{
    public async Task InitializeModules()
    {
        foreach (IModuleInitializerMarker marker in markers)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var initializer = (IModuleInitializer)scope.ServiceProvider.GetRequiredService(marker.Type);
            await initializer.Initialize();
        }
    }
}

#if DEBUG
[assembly: System.Reflection.Metadata.MetadataUpdateHandlerAttribute(typeof(StartSch.HotReloadHandler))]
namespace StartSch;

public class HotReloadHandler
{
    public static event Action<Type[]?>? ClearCacheEvent;
    public static event Action<Type[]?>? UpdateApplicationEvent;

    internal static void ClearCache(Type[]? types) => ClearCacheEvent?.Invoke(types);
    internal static void UpdateApplication(Type[]? types) => UpdateApplicationEvent?.Invoke(types);
}
#endif

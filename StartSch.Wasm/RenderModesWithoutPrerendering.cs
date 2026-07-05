using Microsoft.AspNetCore.Components.Web;

namespace StartSch.Wasm;

public static class RenderModesWithoutPrerendering
{
    public static InteractiveServerRenderMode InteractiveServerWithoutPrerendering { get; } = new(false);
    public static InteractiveWebAssemblyRenderMode InteractiveWebAssemblyWithoutPrerendering { get; } = new(false);
}

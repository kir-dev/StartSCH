using Microsoft.AspNetCore.Components.Web;

namespace StartSch;

public static class RenderModesWithoutPrerendering
{
    public static InteractiveServerRenderMode InteractiveServerNoPrerendering { get; } = new(false);
}

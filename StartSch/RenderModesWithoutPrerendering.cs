using Microsoft.AspNetCore.Components.Web;

namespace StartSch;

public static class RenderModesWithoutPrerendering
{
    public static InteractiveServerRenderMode InteractiveServerWithoutPrerendering { get; } = new(false);
}

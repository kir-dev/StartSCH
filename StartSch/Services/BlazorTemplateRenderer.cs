using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace StartSch.Services;

public class BlazorTemplateRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
{
    public async Task<string> Render<TComponent>(Dictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        // https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-components-outside-of-aspnetcore
        await using var htmlRenderer = new HtmlRenderer(scope.ServiceProvider, loggerFactory);

        return await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameterView = ParameterView.FromDictionary(parameters);
            var root = await htmlRenderer.RenderComponentAsync<TComponent>(parameterView);
            return root.ToHtmlString();
        });
    }
}

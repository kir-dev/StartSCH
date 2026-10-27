using StartSch.Data;

namespace StartSch.Components.Shared;

public static class BlazorUtils
{
    // Usage in a .razor file: <... @key="@article.GetKey()" .../>
    public static string GetKey(this IArticle article)
    {
        return article switch
        {
            Event e => $"event{e.Id}",
            Post p => $"post{p.Id}",
            _ => throw new InvalidOperationException()
        };
    }
}

using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.VikBmeHu;

public class VikBmeHuPollJob(HttpClient httpClient, Db db, IMemoryCache cache) : IPollJobExecutor
{
    private readonly DateTime _utcNow = DateTime.UtcNow;
    
    public async Task Execute(CancellationToken cancellationToken)
    {
        Page page = (await db.Pages
                        .Include(p => p.Categories)
                        .FirstOrDefaultAsync(p => p.ExternalUrl == VikBmeHuModule.Url, cancellationToken))
                    ?? db.Pages.Add(new()
                    {
                        Name = "VIK",
                        ExternalUrl = VikBmeHuModule.Url,
                        Categories =
                        {
                            new()
                            {
                                Interests =
                                {
                                    new ShowPostsInCategory(),
                                    new EmailWhenPostPublishedInCategory(),
                                    new PushWhenPostPublishedInCategory(),
                                }
                            }
                        }
                    }).Entity;
        Category defaultCategory = page.Categories.Single();
        
        int updates = await db.SaveChangesAsync(cancellationToken);
        if (updates > 0)
            cache.Remove(InterestService.CacheKey);

        var externalPosts = await GetExternalPosts(cancellationToken);

        Dictionary<int, Post> externalIdToExternalPost = externalPosts
            .ToDictionary(p => p.ExternalIdInt!.Value);
        var externalIds = externalIdToExternalPost.Keys.ToList();
        Dictionary<int, Post> externalIdToInternalPost = await db.Posts
            .Where(p => p.Categories.Any(c => c.Page == page) && externalIds.Contains(p.ExternalIdInt!.Value))
            .ToDictionaryAsync(p => p.ExternalIdInt!.Value, cancellationToken);

        foreach ((int externalId, Post externalPost) in externalIdToExternalPost)
        {
            if (externalIdToInternalPost.TryGetValue(externalId, out Post? internalPost))
            {
                internalPost.Title = externalPost.Title;
                internalPost.ExcerptMarkdown = externalPost.ExcerptMarkdown;
                internalPost.ContentMarkdown = externalPost.ContentMarkdown;
                internalPost.ExternalUrl = externalPost.ExternalUrl;
                internalPost.Created = externalPost.Created;
                internalPost.Updated = externalPost.Updated;
                internalPost.Published = externalPost.Published;
            }
            else
            {
                defaultCategory.Posts.Add(externalPost);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Post[]> GetExternalPosts(CancellationToken cancellationToken)
    {
        // TODO: Use the HttpClient from DI here
        var config = Configuration.Default.WithDefaultLoader();
        var address = "https://vik.bme.hu/hirek";
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(address, cancellationToken);
        var newsItemsTasks = document
            .QuerySelectorAll(".news-item-list .news-item")
            .Select(async element =>
            {
                var a = element.QuerySelector<IHtmlAnchorElement>("a")!;

                string title = a.TextContent;
                string excerpt = element.QuerySelector(".description")!.InnerHtml;
                string url = a.Href;
                string path = a.PathName;
                var slug = path.RemoveFromStart("/hir/");
                int dash = slug.IndexOf('-');
                int externalId = int.Parse(slug[..dash]);

                string dateString = element.QuerySelector(".date")!.InnerHtml;
                Span<char> dateChars = stackalloc char[dateString.Length];
                dateString.CopyTo(dateChars);
                if (dateChars[^3] == ' ')
                    dateChars[^3] = '0';
                DateTime hungarianDate = DateTime.ParseExact(dateChars, "yyyy. MMMM dd.", Utils.HungarianCulture);
                hungarianDate = hungarianDate.At(12);
                DateTime utcDate = hungarianDate.HungarianToUtc();
                if (utcDate > _utcNow)
                    utcDate = _utcNow;

                var detailsContext = BrowsingContext.New(config);
                var detailsDocument = await detailsContext.OpenAsync(url, cancellationToken);
                string content = detailsDocument.QuerySelector<IHtmlDivElement>(".page-content")!.InnerHtml;

                return new Post()
                {
                    Title = title,
                    ExcerptMarkdown = excerpt,
                    ContentMarkdown = content,
                    Created = utcDate,
                    Updated = utcDate,
                    Published = utcDate,
                    ExternalUrl = url,
                    ExternalIdInt = externalId,
                };
            })
            .ToList();
        return await Task.WhenAll(newsItemsTasks);
    }
}

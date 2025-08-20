using System.Runtime.InteropServices;
using System.Web;
using StartSch.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.BackgroundTasks;
using StartSch.Data;

namespace StartSch.Modules.VikHk;

// https://developer.wordpress.org/rest-api
// https://developer.wordpress.org/rest-api/reference/posts
// https://vik.hk/wp-json/wp/v2/categories
// https://vik.hk/wp-json/wp/v2/posts
public class VikHkPollJob(
    Db db,
    WordPressHttpClient wordPressHttpClient,
    IMemoryCache cache,
    BackgroundTaskManager backgroundTaskManager
) : IPollJobExecutor
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        Page page = await db.Pages
                        .Include(p => p.Categories)
                        .ThenInclude(c => c.Interests)
                        .Include(p => p.Categories)
                        .ThenInclude(c => c.IncludedCategoryIncludes)
                        .Include(p => p.Categories)
                        .ThenInclude(c => c.IncluderCategoryIncludes)
                        .FirstOrDefaultAsync(p => p.PekId == VikHkModule.PekId, cancellationToken)
                    ?? db.Pages.Add(new()
                    {
                        PekId = VikHkModule.PekId,
                        Name = "VIK HK",
                        ExternalUrl = "https://vik.hk",
                        Categories = { new() },
                    }).Entity;
        
        Category defaultCategory = page.Categories.Single(c => c.Name == null);
        
        // [MIGRATION] forgor to add them initially, can be removed when resetting migrations
        if (!defaultCategory.Interests.Any(c => c is EmailWhenPostPublishedInCategory))
        {
            defaultCategory.Interests.Add(new EmailWhenPostPublishedInCategory());
            defaultCategory.Interests.Add(new PushWhenPostPublishedInCategory());
            defaultCategory.Interests.Add(new ShowPostsInCategory());
        }

        List<WordPressCategory> categoryDtos = await wordPressHttpClient.GetCategories();
        categoryDtos = categoryDtos.Where(c => c.Count > 0).ToList();

        Dictionary<int, Category> externalIdToCategory = page.Categories
            .Where(c => c != defaultCategory)
            .ToDictionary(c => c.ExternalIdInt!.Value);
        Dictionary<int, WordPressCategory> externalIdToCategoryDto = categoryDtos
            .ToDictionary(c => c.Id);

        foreach ((int externalId, WordPressCategory dto) in externalIdToCategoryDto)
        {
            ref Category? category = ref CollectionsMarshal.GetValueRefOrAddDefault(
                externalIdToCategory, externalId, out bool exists);

            if (!exists)
            {
                category = new()
                {
                    ExternalIdInt = externalId,
                    Page = page,
                    Interests =
                    {
                        new EmailWhenPostPublishedInCategory(),
                        new PushWhenPostPublishedInCategory(),
                        new ShowPostsInCategory(),
                    },
                };
                page.Categories.Add(category);
            }

            category!.Name = dto.Name;
            category.ExternalUrl = dto.Link;
        }

        HashSet<Category> pageCategories = page.Categories.ToHashSet();
        foreach ((int externalId, WordPressCategory dto) in externalIdToCategoryDto)
        {
            Category category = externalIdToCategory[externalId];
            Category expectedParent = dto.Parent == 0
                ? defaultCategory
                : externalIdToCategory[dto.Parent];

            category.IncluderCategories.RemoveAll(pageCategories.Contains);
            category.IncluderCategories.Add(expectedParent);
        }
        
        int updates = await db.SaveChangesAsync(cancellationToken);
        if (updates > 0)
            cache.Remove(InterestService.CacheKey);

        DateTime lastUpdate = (await db.Posts
                .Where(p => p.Categories.Any(c => c.Page == page))
                .Select(p => p.Updated)
                // ReSharper disable once EntityFramework.UnsupportedServerSideFunctionCall
                .OrderDescending() // supported since EF 9: https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-9.0/whatsnew#translation-of-order-and-orderdescending-linq-operators
                .FirstOrDefaultAsync(cancellationToken))
            .FixDateTimeKind();
        
        if (lastUpdate != default)
            lastUpdate = lastUpdate.AddSeconds(-10); // add some leeway to handle edge cases
        
        List<WordPressPost> modifiedPostDtos =
            await wordPressHttpClient.GetPostsModifiedAfter(lastUpdate, cancellationToken);
        HashSet<int> modifiedExternalIds = modifiedPostDtos.Select(p => p.Id).ToHashSet();

        Dictionary<int, Post> externalIdToModifiedPost = await db.Posts
            .Where(p => modifiedExternalIds.Contains(p.ExternalIdInt!.Value))
            .ToDictionaryAsync(p => p.ExternalIdInt!.Value, cancellationToken);

        List<Post> newPosts = [];
        
        foreach (WordPressPost dto in modifiedPostDtos)
        {
            int externalId = dto.Id;

            if (!externalIdToModifiedPost.TryGetValue(externalId, out Post? post))
            {
                post = new() { ExternalIdInt = externalId };
                newPosts.Add(post);
                db.Posts.Add(post);
            }

            post.Title = HttpUtility.HtmlDecode(dto.Title.Rendered);
            post.ExcerptMarkdown = dto.Excerpt.Rendered;
            post.ContentMarkdown = dto.Content.Rendered;
            post.Created = dto.DateGmt;
            post.Published = dto.DateGmt;
            post.Updated = dto.ModifiedGmt;
            post.ExternalUrl = dto.Link;
            post.Categories.Clear();
            var categories = dto.Categories.Select(cId => externalIdToCategory[cId]).ToList();
            post.Categories.AddRange(categories);
            categories.ForEach(c => c.Posts.Add(post));
        }

        if (newPosts.Count is 1 or 2 or 3)
        {
            DateTime utcNow = DateTime.UtcNow;
            db.CreatePostPublishedNotifications.AddRange(
                newPosts.Select(p => new CreatePostPublishedNotifications() { Created = utcNow, Post = p })
            );
        }

        await db.SaveChangesAsync(cancellationToken);
        
        // remove deleted posts
        HashSet<int> allExternalIds = await wordPressHttpClient.GetPostIds(cancellationToken);
        await db.Posts
            .Where(p => p.Categories.Any(c => c.Page == page) && !allExternalIds.Contains(p.ExternalIdInt!.Value))
            .ExecuteDeleteAsync(cancellationToken);
    }
}

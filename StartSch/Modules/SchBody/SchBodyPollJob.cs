using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.SchBody;

public class SchBodyPollJob(
    Db db,
    NotificationService notificationService,
    NotificationQueueService notificationQueueService
) : IPollJobExecutor
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    record PostEntity(
        int Id,
        string Title,
        string Preview,
        string Content,
        DateTime CreatedAt // UTC
    );

    record PostPaginationEntity(
        List<PostEntity> Data
    );

    public async Task Execute(CancellationToken cancellationToken)
    {
        var response = (await new HttpClient().GetFromJsonAsync<PostPaginationEntity>(
            "https://api.body.kir-dev.hu/posts?page=0&page_size=10000",
            cancellationToken))!;
        Dictionary<string, PostEntity> incoming = response.Data.ToDictionary(GetUrl);

        Page page = await db.Pages
                        .Include(p => p.Categories)
                        .FirstOrDefaultAsync(g => g.PekId == 37, cancellationToken)
                    ?? db.Pages.Add(new() { PekId = 37, PekName = "SCHBody" }).Entity;
        Category category = await db.Categories
                                .Include(c => c.Page)
                                .SingleOrDefaultAsync(c => c.Page == page, cancellationToken)
                            ?? db.Categories.Add(new()
                            {
                                Page = page,
                                Interests =
                                {
                                    new EmailWhenPostPublishedInCategory(),
                                    new PushWhenPostPublishedInCategory(),
                                    new ShowPostsInCategory(),
                                }
                            }).Entity;

        Dictionary<string, Post> posts = page.Id != 0
            ? await db.Posts
                .Where(p => p.Categories.Any(c => c.Id == category.Id))
                .ToDictionaryAsync(p => p.Url!, cancellationToken)
            : [];

        List<Post> requiresNotification = [];

        UpdatePosts(incoming, posts, requiresNotification, db, category);

        // don't create notifications on first startup + fail-safe
        if (requiresNotification.Count > 3)
            requiresNotification.Clear();

        foreach (Post post in requiresNotification)
            await notificationService.CreatePostPublishedNotification(post);

        await db.SaveChangesAsync(cancellationToken);
        if (requiresNotification.Count > 0) notificationQueueService.Notify();
    }

    private static void UpdatePosts(
        Dictionary<string, PostEntity> incoming,
        Dictionary<string, Post> stored,
        List<Post> requiresNotification,
        Db db,
        Category category
    )
    {
        var added = incoming.Keys.Except(stored.Keys).ToHashSet();
        var removed = stored.Keys.Except(incoming.Keys).ToHashSet();
        var modified = stored.Keys.Intersect(incoming.Keys).ToHashSet();

        requiresNotification.AddRange(added.Select(url =>
        {
            PostEntity source = incoming[url];
            return new Post()
            {
                Title = source.Title.Trim(130),
                Url = url,
                ExcerptMarkdown = source.Preview.Trim(1000),
                ContentMarkdown = source.Content.Trim(20000),
                Published = source.CreatedAt,
                Categories = { category },
            };
        }));
        db.Posts.AddRange(requiresNotification);

        db.Posts.RemoveRange(removed.Select(url => stored[url]));

        foreach (var url in modified)
        {
            Post post = stored[url];
            PostEntity source = incoming[url];
            post.Title = source.Title.Trim(130);
            post.ExcerptMarkdown = source.Preview.Trim(1000);
            post.ContentMarkdown = source.Content.Trim(20000);
            post.Published = source.CreatedAt;
        }
    }

    private static string GetUrl(PostEntity post) => $"https://body.sch.bme.hu/#post{post.Id}";
}

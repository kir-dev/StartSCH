using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using StartSch.BackgroundTasks;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Modules.SchBody;

public class SchBodyPollJob(
    HttpClient httpClient,
    Db db,
    BackgroundTaskManager backgroundTaskManager
) : IPollJobExecutor
{
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    record PostEntity(
        int Id,
        string Title,
        string Preview,
        string Content,
        Instant CreatedAt,
        Instant UpdatedAt
    );

    record PostPaginationEntity(
        List<PostEntity> Data
    );

    public async Task Execute(CancellationToken cancellationToken)
    {
        var response = (await httpClient.GetFromJsonAsync<PostPaginationEntity>(
            "https://api.body.kir-dev.hu/posts?page=0&page_size=10000",
            Utils.JsonSerializerOptions,
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
                .ToDictionaryAsync(p => p.ExternalUrl!, cancellationToken)
            : [];

        UpdatePosts(incoming, posts, db, category);

        int rowsAffected = await db.SaveChangesAsync(cancellationToken);
        if (rowsAffected > 0)
            backgroundTaskManager.Notify();
    }

    private static void UpdatePosts(
        Dictionary<string, PostEntity> incoming,
        Dictionary<string, Post> stored,
        Db db,
        Category category
    )
    {
        var added = incoming.Keys.Except(stored.Keys).ToHashSet();
        var removed = stored.Keys.Except(incoming.Keys).ToHashSet();
        var modified = stored.Keys.Intersect(incoming.Keys).ToHashSet();

        List<Post> newPosts = added
            .Select(url =>
            {
                PostEntity source = incoming[url];
                return new Post()
                {
                    Title = source.Title.Trim(130),
                    ExternalUrl = url,
                    ExcerptMarkdown = source.Preview.Trim(1000),
                    ContentMarkdown = source.Content.Trim(20000),
                    Created = source.CreatedAt,
                    Updated = source.UpdatedAt,
                    Published = source.CreatedAt,
                    Categories = { category },
                };
            })
            .ToList();
        db.Posts.AddRange(newPosts);
        if (newPosts.Count is 1 or 2 or 3)
        {
            Instant currentInstant = SystemClock.Instance.GetCurrentInstant();
            db.CreatePostPublishedNotifications.AddRange(
                newPosts.Select(p => new CreatePostPublishedNotifications() { Created = currentInstant, Post = p })
            );
        }

        db.Posts.RemoveRange(removed.Select(url => stored[url]));

        foreach (var url in modified)
        {
            Post post = stored[url];
            PostEntity source = incoming[url];
            post.Title = source.Title.Trim(130);
            post.ExcerptMarkdown = source.Preview.Trim(1000);
            post.ContentMarkdown = source.Content.Trim(20000);
            post.Created = source.CreatedAt;
            post.Updated = source.UpdatedAt;
            post.Published = source.CreatedAt;
        }
    }

    private static string GetUrl(PostEntity post) => $"https://body.sch.bme.hu/#post{post.Id}";
}

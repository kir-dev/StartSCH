using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;
using StartSch.Services;
using StartSch.Wasm;

namespace StartSch.Modules.SchBody;

public class SchBodyPollJob(Db db, NotificationQueueService notificationQueueService) : IPollJobExecutor
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
                                .Include(c => c.Owner)
                                .SingleOrDefaultAsync(c => c.Owner == page, cancellationToken)
                            ?? db.Categories.Add(new() { Owner = page }).Entity;

        Dictionary<string, Post> posts = page.Id != 0
            ? await db.Posts
                .Where(p => p.Categories.Any(g => g.Id == page.Id))
                .ToDictionaryAsync(p => p.Url!, cancellationToken)
            : [];

        List<Post> requiresNotification = [];

        UpdatePosts(incoming, posts, requiresNotification, db, category);

        // don't create notifications on first startup + fail-safe
        if (requiresNotification.Count > 3)
            requiresNotification.Clear();

        if (requiresNotification.Count > 0)
        {
            DateTime utcNow = DateTime.UtcNow;

            Interest interest = new CategoryInterest() { Category = category };

            foreach (Post post in requiresNotification)
            {
                Notification notification = new PostNotification() { Post = post };

                // var pushTargets = TagGroup.GetAllTargets(["push.schbody.hírek"]);
                // var pushUsers = await db.Users
                //     .Where(u => u.Tags.Any(t => pushTargets.Contains(t.Path)))
                //     .ToListAsync(cancellationToken);
                // notification.Requests.AddRange(
                //     pushUsers.Select(u =>
                //         new PushRequest
                //         {
                //             CreatedUtc = utcNow,
                //             Notification = notification,
                //             User = u,
                //         })
                // );
                //
                // var emailTargets = TagGroup.GetAllTargets(["email.schbody.hírek"]);
                // var emailUsers = await db.Users
                //     .Where(u => u.Tags.Any(t => emailTargets.Contains(t.Path)))
                //     .ToListAsync(cancellationToken);
                // notification.Requests.AddRange(
                //     emailUsers.Select(u =>
                //         new EmailRequest()
                //         {
                //             CreatedUtc = utcNow,
                //             Notification = notification,
                //             User = u,
                //         })
                // );
                //
                // db.Notifications.Add(notification);
            }
        }

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
                PublishedUtc = source.CreatedAt,
                CreatedUtc = source.CreatedAt,
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
            post.PublishedUtc = source.CreatedAt;
        }
    }

    private static string GetUrl(PostEntity post) => $"https://body.sch.bme.hu/#post{post.Id}";
}

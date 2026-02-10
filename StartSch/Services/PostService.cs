using Microsoft.EntityFrameworkCore;
using StartSch.BackgroundTasks;
using StartSch.Data;

namespace StartSch.Services;

public class PostService(
    Db db,
    AuthorizationService authorizationService,
    BackgroundTaskManager backgroundTaskManager
)
{
    public async Task<Post> Save(
        int postId,
        int? eventId,
        HashSet<int> categoryIds,
        string title,
        string? contentMd,
        string? excerptMd,
        PostAction action)
    {
        Post post;

        if (postId == 0)
        {
            post = new()
            {
                Title = title,
                ExcerptMarkdown = excerptMd,
                ContentMarkdown = contentMd,
                Created = SystemClock.Instance.GetCurrentInstant(),
                Event = eventId.HasValue
                    ? await db.Events
                        .Include(e => e.Categories)
                        .FirstAsync(e => e.Id == eventId)
                    : null
            };

            post.Categories.AddRange(
                await db.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToListAsync()
            );

            authorizationService.CheckCreate(post);

            db.Posts.Add(post);
        }
        else
        {
            post = await db.Posts
                .Include(p => p.Categories)
                .Include(p => p.Event)
                .ThenInclude(e => e!.Categories)
                .FirstAsync(p => p.Id == postId);

            var newEvent = eventId == null
                ? null
                : post.Event is { } existingEvent && existingEvent.Id == eventId
                    ? existingEvent
                    : await db.Events
                        .Include(e => e.Categories)
                        .FirstAsync(e => e.Id == eventId);
            var newCategories = await db.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync();

            authorizationService.CheckUpdate(post, newEvent, newCategories);

            post.Event = newEvent;
            post.Categories.Clear();
            post.Categories.AddRange(newCategories);

            post.Title = title;
            post.ExcerptMarkdown = excerptMd;
            post.ContentMarkdown = contentMd;
        }

        if (action == PostAction.Publish)
        {
            if (post.Published.HasValue)
                throw new InvalidOperationException("Post is already published.");
            post.Published = SystemClock.Instance.GetCurrentInstant();
            db.CreatePostPublishedNotifications.Add(new() { Created = SystemClock.Instance.GetCurrentInstant(), Post = post });
        }

        await db.SaveChangesAsync();

        if (action == PostAction.Publish)
            backgroundTaskManager.Notify();

        return post;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StartSch.Authorization.Requirements;
using StartSch.Data;
using StartSch.Wasm;

namespace StartSch.Services;

public class PostService(
    Db db,
    IAuthorizationService authorizationService,
    NotificationService notificationService,
    NotificationQueueService notificationQueueService)
{
    public async Task<Post> Save(
        ClaimsPrincipal user,
        int postId,
        int? eventId,
        List<int> categoryIds,
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
                Created = DateTime.UtcNow,
            };

            db.Posts.Add(post);
        }
        else
        {
            post = await db.Posts
                       .Include(p => p.Categories)
                       .ThenInclude(c => c.Page)
                       .Include(p => p.Event)
                       .ThenInclude(e => e!.Categories)
                       .ThenInclude(c => c.Page)
                       .FirstOrDefaultAsync(p => p.Id == postId)
                   ?? throw new InvalidOperationException();

            var canUpdate = await authorizationService.AuthorizeAsync(user, post, ResourceAccessRequirement.Write);
            if (!canUpdate.Succeeded) throw new InvalidOperationException();

            post.Title = title;
            post.ExcerptMarkdown = excerptMd;
            post.ContentMarkdown = contentMd;
        }

        if (action == PostAction.Publish)
            post.Published = DateTime.UtcNow;

        Event? newEvent = eventId.HasValue
            ? await db.Events
                  .Include(e => e.Categories)
                  .ThenInclude(c => c.Page)
                  .FirstOrDefaultAsync(e => e.Id == eventId)
              ?? throw new InvalidOperationException()
            : null;

        List<Category> newCategories = await db.Categories
            .Include(c => c.Page)
            .Where(g => categoryIds.Contains(g.Id))
            .ToListAsync();

        List<Page> oldOwners = post.GetOwners();
        List<Page> newOwners = newCategories.Select(c => c.Page).Distinct().ToList();
        
        if (newCategories.Count == 0) throw new InvalidOperationException();
        if (newEvent == null)
        {
            // either only have a single page or all pages must already be associated with the post
            bool isValid = newOwners.Count == 1 || newOwners.All(oldOwners.Contains);
            if (!isValid) throw new InvalidOperationException();
        }
        else
        {
            List<Page> newEventOwners = newEvent.GetOwners();
            
            // every page must already be associated with either the event or the post
            bool isValid = newOwners.All(g => newEventOwners.Contains(g) || oldOwners.Contains(g));
            if (!isValid) throw new InvalidOperationException();
        }
        post.Categories.Clear();
        post.Categories.AddRange(newCategories);

        if (post.EventId != eventId)
        {
            if (newEvent != null)
            {
                var canAddToNewEvent = await authorizationService.AuthorizeAsync(
                    user, newEvent, ResourceAccessRequirement.Write);
                if (!canAddToNewEvent.Succeeded) throw new InvalidOperationException();
            }
            // can remove from event regardless of access to it. the last authorization call (canSave) checks
            // whether the user still has access to the post without the removed event

            post.Event = newEvent;
        }

        var canSave = await authorizationService.AuthorizeAsync(user, post, ResourceAccessRequirement.Write);
        if (!canSave.Succeeded) throw new InvalidOperationException();

        if (action == PostAction.Publish)
            await notificationService.CreatePostPublishedNotification(post);

        await db.SaveChangesAsync();

        notificationQueueService.Notify();

        return post;
    }
}

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
    NotificationQueueService notificationQueueService)
{
    public async Task<Post> Save(
        ClaimsPrincipal user,
        int postId,
        int? eventId,
        List<int> groupIds,
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
                CreatedUtc = DateTime.UtcNow,
            };

            db.Posts.Add(post);
        }
        else
        {
            post = await db.Posts
                       .Include(p => p.Groups)
                       .Include(p => p.Event)
                       .FirstOrDefaultAsync(p => p.Id == postId)
                   ?? throw new InvalidOperationException();

            var canUpdate = await authorizationService.AuthorizeAsync(user, post, ResourceAccessRequirement.Write);
            if (!canUpdate.Succeeded) throw new InvalidOperationException();

            post.Title = title;
            post.ExcerptMarkdown = excerptMd;
            post.ContentMarkdown = contentMd;
        }

        if (action == PostAction.Publish)
            post.PublishedUtc = DateTime.UtcNow;

        var newEvent = eventId.HasValue
            ? await db.Events
                  .Include(e => e.Groups)
                  .FirstOrDefaultAsync(e => e.Id == eventId)
              ?? throw new InvalidOperationException()
            : null;

        List<Page> newGroups = await db.Groups
            .Where(g => groupIds.Contains(g.Id))
            .ToListAsync();
        if (newGroups.Count == 0) throw new InvalidOperationException();
        if (newEvent == null)
        {
            // either only have a single group or all groups must already have access to the post
            bool isValid = newGroups.Count == 1 || newGroups.All(g => post.Groups.Contains(g));
            if (!isValid) throw new InvalidOperationException();
        }
        else
        {
            // every group must already have access to the event or the post
            bool isValid = newGroups.All(g => newEvent.Groups.Contains(g) || post.Groups.Contains(g));
            if (!isValid) throw new InvalidOperationException();
        }
        post.Groups.Clear();
        post.Groups.AddRange(newGroups);

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
        {
            var pincerGroups = post.Groups.Where(g => g.PincerName != null).ToList();
            if (pincerGroups.Count > 0)
            {
                Notification notification = new PostNotification() { Post = post };

                DateTime utcNow = DateTime.UtcNow;

                var pushTags = pincerGroups.Select(g => $"push.pincér.hírek.{g.PincerName!}");
                var pushTargets = TagGroup.GetAllTargets(pushTags);
                var pushUsers = await db.Users
                    .Where(u => u.Tags.Any(t => pushTargets.Contains(t.Path)))
                    .ToListAsync();
                notification.Requests.AddRange(
                    pushUsers.Select(u =>
                        new PushRequest
                        {
                            CreatedUtc = utcNow,
                            Notification = notification,
                            User = u,
                        })
                );

                var emailTags = pincerGroups.Select(g => $"email.pincér.hírek.{g.PincerName!}");
                var emailTargets = TagGroup.GetAllTargets(emailTags);
                var emailUsers = await db.Users
                    .Where(u => u.Tags.Any(t => emailTargets.Contains(t.Path)))
                    .ToListAsync();
                notification.Requests.AddRange(
                    emailUsers.Select(u =>
                        new EmailRequest()
                        {
                            CreatedUtc = utcNow,
                            Notification = notification,
                            User = u,
                        })
                );

                db.Notifications.Add(notification);
            }
        }

        await db.SaveChangesAsync();

        notificationQueueService.Notify();

        return post;
    }
}

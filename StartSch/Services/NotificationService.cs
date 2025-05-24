using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class NotificationService(Db db)
{
    public async Task CreatePostPublishedNotification(Post post)
    {
        HashSet<Category> targetCategories = CategoryUtils.FlattenIncludingCategories(post.Categories);
        List<Interest> interests = await db.Interests
            .Include(i => i.Subscriptions)
            .Where(i =>
                (
                    i is EmailWhenPostPublishedInCategory || i is PushWhenPostPublishedInCategory)
                    && targetCategories.Contains(((CategoryInterest)i).Category
                )
                ||
                (
                    (i is EmailWhenPostPublishedForEvent || i is PushWhenPostPublishedForEvent)
                    && ((EventInterest)i).Event == post.Event
                )
            )
            .ToListAsync();

        Notification notification = new PostNotification() { Post = post, };
        
        AddRequests(notification.Requests, interests);

        db.Notifications.Add(notification);
    }

    public async Task CreateOrderingStartedNotification(PincerOpening opening)
    {
        HashSet<Category> targetCategories = CategoryUtils.FlattenIncludingCategories(opening.Categories);
        
        List<Interest> interests = await db.Interests
            .Include(i => i.Subscriptions)
            .Where(i =>
                (i is EmailWhenOrderingStartedInCategory || i is PushWhenOrderingStartedInCategory)
                && targetCategories.Contains(((CategoryInterest)i).Category)
            )
            .ToListAsync();

        Notification notification = new OrderingStartedNotification() { Opening = opening, };
        
        AddRequests(notification.Requests, interests);

        db.Notifications.Add(notification);
    }

    private static void AddRequests(List<NotificationRequest> requests, List<Interest> interests)
    {
        // ensure we don't send the same notification twice to the same user
        Dictionary<int, EmailRequest> emailRequests = [];
        Dictionary<int, PushRequest> pushRequests = [];
        
        DateTime utcNow = DateTime.UtcNow;
        
        foreach (var interest in interests)
        {
            switch (interest)
            {
                case PushWhenPostPublishedForEvent or PushWhenPostPublishedInCategory or PushWhenOrderingStartedInCategory:
                    interest.Subscriptions.ForEach(subscription => pushRequests.TryAdd(
                        subscription.UserId,
                        new() { CreatedUtc = utcNow, UserId = subscription.UserId }
                    ));
                    break;
                case EmailWhenPostPublishedForEvent or EmailWhenPostPublishedInCategory or EmailWhenOrderingStartedInCategory:
                    interest.Subscriptions.ForEach(subscription => emailRequests.TryAdd(
                        subscription.UserId,
                        new() { CreatedUtc = utcNow, UserId = subscription.UserId }
                    ));
                    break;
            }
        }
        
        requests.AddRange(pushRequests.Values);
        requests.AddRange(emailRequests.Values);
    }
}

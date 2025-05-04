using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class NotificationService(Db db)
{
    public async Task CreatePostPublishedNotification(Post post)
    {
        HashSet<Category> targetCategories = CategoryUtils.FlattenIncludingCategories(post.Categories);
        List<InterestSubscription> subscriptions = await db.InterestSubscriptions
            .Where(s => s is EmailInterestSubscription || s is PushInterestSubscription)
            .Where(s =>
                targetCategories.Contains(
                    ((CategoryInterest)s.Interest).Category
                )
            )
            .ToListAsync();
        
        Notification notification = new PostNotification() { Post = post, };
        notification.Requests.AddRange(
            subscriptions.Select<InterestSubscription, NotificationRequest>(interestSubscription =>
                interestSubscription switch
                {
                    PushInterestSubscription => new PushRequest()
                    {
                        UserId = interestSubscription.UserId,
                        CreatedUtc = DateTime.UtcNow,
                    },
                    EmailInterestSubscription => new EmailRequest()
                    {
                        UserId = interestSubscription.UserId,
                        CreatedUtc = DateTime.UtcNow,
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(interestSubscription), interestSubscription, null)
                }
            )
        );
        
        db.Notifications.Add(notification);
    }
}

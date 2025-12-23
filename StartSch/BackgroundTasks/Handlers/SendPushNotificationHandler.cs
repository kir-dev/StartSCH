using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Services;
using PushSubscription = StartSch.Data.PushSubscription;

namespace StartSch.BackgroundTasks.Handlers;

public class SendPushNotificationHandler(
    Db db,
    IMemoryCache cache,
    PushServiceClient pushServiceClient
)
    : IBackgroundTaskHandler<SendPushNotification>
{
    public async Task Handle(List<SendPushNotification> batch, CancellationToken cancellationToken)
    {
        int taskId = batch.Single().Id;
        var task = await db.SendPushNotifications
            .Include(x => x.User.PushSubscriptions)
            .Include(x => x.Message)
            .FirstAsync(x => x.Id == taskId, cancellationToken);
        
        Duration? ttl = task.Message.ValidUntil is {} validUntil
            ? validUntil - SystemClock.Instance.GetCurrentInstant()
            : null;
        if (ttl < Duration.FromSeconds(10))
            return;
        
        var subscriptions = task.User.PushSubscriptions;
        foreach (PushSubscription subscription in subscriptions)
        {
            Lib.Net.Http.WebPush.PushSubscription pushSubscription = new() { Endpoint = subscription.Endpoint };
            pushSubscription.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);
            pushSubscription.SetKey(PushEncryptionKeyName.P256DH, subscription.P256DH);
            try
            {
                await pushServiceClient.RequestPushMessageDeliveryAsync(
                    pushSubscription,
                    new(task.Message.Payload)
                    {
                        Topic = task.Message.Topic,
                        Urgency = task.Message.Urgency ?? PushMessageUrgency.Low,
                        TimeToLive = (int?)ttl?.TotalSeconds
                    },
                    cancellationToken);
            }
            catch (PushServiceClientException)
            {
                // invalid subscription, delete
                await db.PushSubscriptions
                    .Where(p => p.Endpoint == subscription.Endpoint)
                    .ExecuteDeleteAsync(cancellationToken);
                cache.Remove(PushSubscriptionService.GetPushEndpointsCacheKey(subscription.UserId));
            }
        }
    }
}

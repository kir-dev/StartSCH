using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using PushSubscription = StartSch.Data.PushSubscription;

namespace StartSch.Services;

public class NotificationQueueService(
    IDbContextFactory<Db> dbFactory,
    PushServiceClient pushServiceClient,
    IMemoryCache cache)
    : BackgroundService
{
    private volatile TaskCompletionSource _taskCompletionSource = new();

    public void Notify() => _taskCompletionSource.TrySetResult();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Notify();

        while (!stoppingToken.IsCancellationRequested)
        {
            await _taskCompletionSource.Task;
            _taskCompletionSource = new();

            await using Db db = await dbFactory.CreateDbContextAsync(stoppingToken);
            var userPushNotificationRequests = await db.UserPushMessageRequests
                .Include(r => r.PushMessage)
                .Include(r => r.User.PushSubscriptions)
                .Take(50)
                .ToListAsync(stoppingToken);
            // TODO: Perf: improve UserEmailRequests query
            // Currently, this duplicates the email's content for every user that will receive it.
            // The above query for push notifications could be improved accordingly.
            // https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries#data-duplication
            var userEmailRequests = await db.UserEmailRequests
                .Include(r => r.Email)
                .Include(r => r.User)
                .Take(50)
                .ToListAsync(stoppingToken);
            await Task.WhenAll(
                userPushNotificationRequests.Select(async request =>
                    {
                        var notification = request.PushMessage;
                        var subscriptions = request.User.PushSubscriptions;
                        await using Db perRequestDb = await dbFactory.CreateDbContextAsync(stoppingToken);
                        foreach (PushSubscription subscription in subscriptions)
                        {
                            Lib.Net.Http.WebPush.PushSubscription pushSubscription = new()
                                { Endpoint = subscription.Endpoint };
                            pushSubscription.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);
                            pushSubscription.SetKey(PushEncryptionKeyName.P256DH, subscription.P256DH);
                            try
                            {
                                await pushServiceClient.RequestPushMessageDeliveryAsync(
                                    pushSubscription,
                                    new(notification.Data),
                                    stoppingToken);
                            }
                            catch (PushServiceClientException e)
                            {
                                await perRequestDb.PushSubscriptions
                                    .Where(p => p.Endpoint == subscription.Endpoint)
                                    .ExecuteDeleteAsync(stoppingToken);
                                cache.Remove(nameof(PushSubscriptionState) + subscription.UserId);
                            }
                        }

                        await perRequestDb.UserPushMessageRequests
                            .Where(r => r.Id == request.Id)
                            .ExecuteDeleteAsync(stoppingToken);
                    }
                )

                // TODO: send emails
            );

            await db.Emails
                .Where(e => !e.Requests.Any())
                .ExecuteDeleteAsync(stoppingToken);
            await db.PushMessages
                .Where(e => !e.Requests.Any())
                .ExecuteDeleteAsync(stoppingToken);
        }
    }
}
using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Components.Shared;
using StartSch.Data;
using PushSubscription = StartSch.Data.PushSubscription;

namespace StartSch.Services;

public class NotificationQueueService(
    IDbContextFactory<Db> dbFactory,
    PushServiceClient pushServiceClient,
    BlazorTemplateRenderer templateRenderer,
    IEmailService emailService,
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
            await _taskCompletionSource.Task.WaitAsync(stoppingToken);
            _taskCompletionSource = new();

            await using Db db = await dbFactory.CreateDbContextAsync(stoppingToken);
            var userPushNotificationRequests = await db.UserPushMessageRequests
                .Include(r => r.PushMessage)
                .Include(r => r.User.PushSubscriptions)
                .Take(50)
                .ToListAsync(stoppingToken);
            // TODO: Perf: improve UserEmailRequests query
            // Currently, this duplicates the email's content for every user that will receive it.
            // The above query for push notifications will have to be improved accordingly.
            // https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries#data-duplication
            var userEmailRequests = await db.UserEmailRequests
                .Include(r => r.Email)
                .Include(r => r.User)
                .Take(50)
                .ToListAsync(stoppingToken);
            await Task.WhenAll(
                userPushNotificationRequests
                    .Select(async request =>
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
                            catch (PushServiceClientException)
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
                    })
                    .Concat(
                        userEmailRequests
                            .GroupBy(r => r.Email)
                            .Select(async group =>
                            {
                                var addresses = group
                                    .Select(r => r.User is { StartSchEmail: { } addr, StartSchEmailVerified: true }
                                        ? addr
                                        : r.User.AuthSchEmail)
                                    .Where(a => a != null)
                                    .Select(a => a!)
                                    .ToList();

                                var email = group.Key;

                                string content = await templateRenderer.Render<EmailTemplate>(new()
                                {
                                    { nameof(EmailTemplate.HtmlContent), email.ContentHtml },
                                    { nameof(EmailTemplate.PostId), email.PostId }
                                });

                                await emailService.Send(email.From, addresses, email.Subject, content);

                                await using Db perRequestDb = await dbFactory.CreateDbContextAsync(stoppingToken);
                                perRequestDb.UserEmailRequests.RemoveRange(group);
                                await perRequestDb.SaveChangesAsync(stoppingToken);
                            })
                    )
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
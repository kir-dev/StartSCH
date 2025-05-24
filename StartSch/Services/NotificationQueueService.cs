using System.Data;
using System.Diagnostics;
using System.Text.Json;
using JetBrains.Annotations;
using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Components.EmailTemplates;
using StartSch.Data;
using PushSubscription = StartSch.Data.PushSubscription;

namespace StartSch.Services;

public class NotificationQueueService(
    IDbContextFactory<Db> dbFactory,
    IServiceProvider serviceProvider,
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
            // Wait for the next notification if there is no more work to do
            await _taskCompletionSource.Task.WaitAsync(stoppingToken);

            await using var scope = serviceProvider.CreateAsyncScope();

            Db db = scope.ServiceProvider.GetRequiredService<Db>();
            InterestService interestService = scope.ServiceProvider.GetRequiredService<InterestService>();

            // required because of the split query https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
            await using var tx = await db.BeginTransaction(IsolationLevel.Snapshot, stoppingToken);

            await interestService.LoadIndex();

            var requests = await db.NotificationRequests
                .Include(r => ((PostNotification)r.Notification).Post.Event)
                .Include(r => ((PostNotification)r.Notification).Post.PostCategories)
                .Include(r => ((OrderingStartedNotification)r.Notification).Opening.EventCategories)
                .Include(r => r.User.PushSubscriptions)
                .OrderBy(r => r.Id)
                .Take(50)
                .AsSplitQuery()
                .ToListAsync(stoppingToken);

            await tx.CommitAsync(stoppingToken);

            if (requests.Count == 0)
            {
                _taskCompletionSource = new();
                continue;
            }

            await Task.WhenAll(
                requests
                    .OfType<PushRequest>()
                    .Select(async request =>
                    {
                        var notification = request.Notification;
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
                                    new(GetPushMessageBody(notification)),
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

                        await perRequestDb.PushRequests
                            .Where(r => r.Id == request.Id)
                            .ExecuteDeleteAsync(stoppingToken);
                    })
                    .Concat(
                        requests
                            .OfType<EmailRequest>()
                            .GroupBy(r => r.Notification)
                            .Select(async group =>
                            {
                                Notification notification = group.Key;
                                List<User> users = group.Select(r => r.User).ToList();
                                MultipleSendRequestDto sendRequest = await GetEmailSendRequest(notification, users);

                                await emailService.Send(sendRequest);

                                await using Db perRequestDb = await dbFactory.CreateDbContextAsync(stoppingToken);
                                perRequestDb.EmailRequests.RemoveRange(group);
                                await perRequestDb.SaveChangesAsync(stoppingToken);
                            })
                    )
            );

            await db.Notifications
                .Where(n => !n.Requests.Any())
                .ExecuteDeleteAsync(stoppingToken);
        }
    }

    private static string GetPushMessageBody(Notification notification)
    {
        return JsonSerializer.Serialize(
            notification switch
            {
                OrderingStartedNotification orderingStartedNotification => GetPushNotification(orderingStartedNotification),
                PostNotification postNotification => GetPushNotification(postNotification.Post),
                _ => throw new ArgumentOutOfRangeException(nameof(notification))
            },
            JsonSerializerOptions.Web
        );
    }

    private static PushNotificationDto GetPushNotification(OrderingStartedNotification notification)
    {
        return new(
            "Rendelhető: " + string.Join(" × ", notification.Opening.GetOwners().Select(g => g.PincerName ?? g.PekName)),
            notification.Opening.Title,
            $"/events/{notification.Opening.Id}",
            null
        );
    }

    private static PushNotificationDto GetPushNotification(Post post)
    {
        TextContent textContent = new(post.ContentMarkdown, post.ExcerptMarkdown);
        return new(
            post.Title,
            textContent.TextExcerpt,
            $"/posts/{post.Id}",
            null
        );
    }

    [UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
    private record PushNotificationDto(
        string Title,
        string? Body,
        string Url,
        string? Icon
    );

    private Task<MultipleSendRequestDto> GetEmailSendRequest(Notification notification, List<User> users)
    {
        return notification switch
        {
            OrderingStartedNotification => throw new UnreachableException(),
            PostNotification postNotification => GetEmailSendRequest(postNotification.Post, users),
            _ => throw new ArgumentOutOfRangeException(nameof(notification))
        };
    }

    private async Task<MultipleSendRequestDto> GetEmailSendRequest(Post post, List<User> users)
    {
        var content = await templateRenderer.Render<PostEmailTemplate>(
            new() { { nameof(PostEmailTemplate.Post), post } });
        var to = users
            .Select(u => u.GetVerifiedEmailAddress())
            .Where(a => a != null)
            .Select(a => a!)
            .ToList();
        var from = string.Join(", ", post.GetOwners().Select(g => g.PincerName ?? g.PekName));
        return new(
            new(from, null),
            to,
            post.Title,
            content
        );
    }
}

using System.Text.Json;
using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using StartSch.Components.EmailTemplates;
using StartSch.Data;
using StartSch.Services;

// ReSharper disable EntityFramework.NPlusOne.IncompleteDataQuery
// ReSharper disable EntityFramework.NPlusOne.IncompleteDataUsage

namespace StartSch.BackgroundTasks.Handlers;

public class CreatePostPublishedNotificationsHandler(
    Db db,
    InterestService interestService,
    BlazorTemplateRenderer templateRenderer,
    BackgroundTaskManager backgroundTaskManager
)
    : IBackgroundTaskHandler<CreatePostPublishedNotifications>
{
    public async Task Handle(List<CreatePostPublishedNotifications> batch, CancellationToken cancellationToken)
    {
        var request = batch.Single();

        await interestService.LoadIndex;

        var post = await db
            .Posts
            .Include(x => x.PostCategories)
            .FirstAsync(x => x.Id == request.PostId, cancellationToken);
        var baseCategories = post.Categories;
        var allCategories = CategoryUtils.FlattenIncludingCategories(baseCategories);
        var interests = allCategories.SelectMany(c => c.Interests).ToList();

        var pushInterests = interests.Where(i => i is PushWhenPostPublishedInCategory).ToList();
        var emailInterests = interests.Where(i => i is EmailWhenPostPublishedInCategory).ToList();

        List<int> pushUserIds = await db.Interests
            .Where(i => pushInterests.Contains(i))
            .SelectMany(i => i.Subscribers)
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
        List<int> emailUserIds = await db.Interests
            .Where(i => emailInterests.Contains(i))
            .SelectMany(i => i.Subscribers)
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (pushUserIds.Count != 0 || emailUserIds.Count != 0)
        {
            TextContent textContent = new(post.ContentMarkdown, post.ExcerptMarkdown);

            string from = string.Join(',', post.Categories.GetOwners().Select(x => x.GetName()));

            PushNotificationMessage pushNotificationMessage = new()
            {
                Payload = JsonSerializer.Serialize(new PushNotificationDto(
                    post.Title,
                    $"({from}) {textContent.TextExcerpt}",
                    $"/posts/{post.Id}",
                    null
                ), Utils.JsonSerializerOptions),
                Topic = $"post{post.Id}",
                Urgency = PushMessageUrgency.Normal,
                ValidUntil = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(7)),
            };

            string emailContent = await templateRenderer.Render<PostEmailTemplate>(new()
            {
                { nameof(PostEmailTemplate.Post), post }
            });
            EmailMessage emailMessage = new()
            {
                FromName = from,
                ContentHtml = emailContent,
                Subject = post.Title,
            };

            Instant instant = SystemClock.Instance.GetCurrentInstant();
            db.BackgroundTasks.AddRange(pushUserIds.Select(i =>
                new SendPushNotification() { UserId = i, Message = pushNotificationMessage, Created = instant }));
            db.BackgroundTasks.AddRange(emailUserIds.Select(i =>
                new SendEmail() { UserId = i, Message = emailMessage, Created = instant }));
        }
        
        db.BackgroundTasks.Remove(request);
        await db.SaveChangesAsync(cancellationToken);
        backgroundTaskManager.Notify();
    }
}

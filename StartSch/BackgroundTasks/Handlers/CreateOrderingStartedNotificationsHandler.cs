using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StartSch.Components.EmailTemplates;
using StartSch.Data;
using StartSch.Services;

// ReSharper disable EntityFramework.NPlusOne.IncompleteDataQuery
// ReSharper disable EntityFramework.NPlusOne.IncompleteDataUsage

namespace StartSch.BackgroundTasks.Handlers;

public class CreateOrderingStartedNotificationsHandler(
    Db db,
    InterestService interestService,
    BlazorTemplateRenderer templateRenderer,
    BackgroundTaskManager backgroundTaskManager
)
    : IBackgroundTaskHandler<CreateOrderingStartedNotifications>
{
    public async Task Handle(List<CreateOrderingStartedNotifications> batch, CancellationToken cancellationToken)
    {
        var request = batch.Single();

        var index = await interestService.LoadIndex;

        var opening = await db
            .PincerOpenings
            .Include(x => x.EventCategories)
            .FirstAsync(x => x.Id == request.PincerOpeningId, cancellationToken);
        List<int> baseCategoryIds = [opening.Categories[0].Id];
        var baseCategories = index.GetCategories(baseCategoryIds);
        var allCategories = CategoryUtils.FlattenIncludingCategories(baseCategories);
        var interests = allCategories.SelectMany(c => c.Interests).ToList();

        var pushInterests = interests.Where(i => i is PushWhenOrderingStartedInCategory).ToList();
        var emailInterests = interests.Where(i => i is EmailWhenOrderingStartedInCategory).ToList();

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

        Page page = opening.Categories[0].Page;

        string title = "Rendelhető: " + page.GetName();
        
        PushNotificationMessage pushMessage = new()
        {
            Payload = JsonSerializer.Serialize(new PushNotificationDto(
                title,
                opening.Title,
                $"/events/{opening.Id}",
                null
            ), JsonSerializerOptions.Web)
        };

        string emailContent = await templateRenderer.Render<OrderingStartedEmailTemplate>(new()
        {
            { nameof(OrderingStartedEmailTemplate.PincerOpening), opening }
        });
        EmailMessage emailMessage = new()
        {
            FromName = page.GetName(),
            FromEmail = "",
            ContentHtml = emailContent,
            Subject = title,
        };

        DateTime utcNow = DateTime.UtcNow;
        db.BackgroundTasks.AddRange(pushUserIds.Select(i =>
            new SendPushNotification() { UserId = i, Message = pushMessage, Created = utcNow }));
        db.BackgroundTasks.AddRange(emailUserIds.Select(i =>
            new SendEmail() { UserId = i, Message = emailMessage, Created = utcNow }));
        db.BackgroundTasks.Remove(request);

        await db.SaveChangesAsync(cancellationToken);

        backgroundTaskManager.Notify();
    }
}

using StartSch.Data;
using StartSch.Services;

namespace StartSch.BackgroundTasks.Handlers;

public class CreateOrderingStartedNotificationsHandler(
    Db db,
    NotificationService notificationService
)
    : IBackgroundTaskHandler<CreateOrderingStartedNotifications>
{
    public async Task Handle(List<CreateOrderingStartedNotifications> batch, CancellationToken cancellationToken)
    {
        var request = batch.Single();
        await notificationService.CreateOrderingStartedNotification(request.PincerOpening);
        db.BackgroundTasks.Remove(request);
        await db.SaveChangesAsync(cancellationToken);
    }
}

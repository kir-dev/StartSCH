using StartSch.Data;
using StartSch.Services;

namespace StartSch.BackgroundTasks.Handlers;

public class SendPushNotificationHandler(IEmailService emailService)
    : IBackgroundTaskHandler<SendPushNotification>
{
    public async Task Handle(List<SendPushNotification> batch, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // TODO: load relationships, group by notification, send
    }
}

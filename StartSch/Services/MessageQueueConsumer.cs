using StartSch.Data;

namespace StartSch.Services;

public abstract class BackgroundTask
{
    private BackgroundTask() {}
    
    public int Id { get; set; }

    public class SendPushNotification : BackgroundTask
    {
        public User User { get; set; }
    }

    public class SendEmail : BackgroundTask
    {
        public User User { get; set; }
        public Notification Notification { get; set; }
    }

    public class CreatePostPublishedNotifications : BackgroundTask
    {
        public Post Post { get; set; }
    }

    public class CreateEventStartedNotifications : BackgroundTask
    {
    }

    public class CreateOrderingStartedNotifications : BackgroundTask
    {
    }
}

public class MessageQueueConsumer(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        Db db = scope.ServiceProvider.GetRequiredService<Db>();
    }
}

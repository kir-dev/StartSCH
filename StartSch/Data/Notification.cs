namespace StartSch.Data;

public abstract class Notification
{
    public int Id { get; init; }

    public List<NotificationRequest> Requests { get; } = [];
}

public class OrderingStartedNotification : Notification
{
    public required Opening Opening { get; init; }
}

public class PostNotification : Notification
{
    public required Post Post { get; init; }
}

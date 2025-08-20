namespace StartSch.Data;

public abstract class Notification
{
    public int Id { get; init; }

    public List<NotificationRequest> Requests { get; } = [];
}

public class EventStartedNotification : Notification
{
    public required Event Event { get; set; }
}

public class OrderingStartedNotification : Notification
{
    public required PincerOpening Opening { get; init; }
}

public class PostPublishedNotification : Notification
{
    public required Post Post { get; init; }
}

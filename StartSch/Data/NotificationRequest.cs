namespace StartSch.Data;

public class PushRequest : NotificationRequest
{
}

public class EmailRequest : NotificationRequest
{
}

public abstract class NotificationRequest
{
    public int Id { get; init; }

    public required DateTime CreatedUtc { get; init; }

    public Notification Notification { get; init; } = null!;
    public required User User { get; init; }
}

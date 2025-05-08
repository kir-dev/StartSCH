namespace StartSch.Data;

public abstract class NotificationRequest
{
    public int Id { get; init; }
    public int UserId { get; init; }

    public required DateTime CreatedUtc { get; init; }

    public Notification Notification { get; init; } = null!;
    public User User { get; init; } = null!;
}

public class PushRequest : NotificationRequest
{
}

public class EmailRequest : NotificationRequest
{
}

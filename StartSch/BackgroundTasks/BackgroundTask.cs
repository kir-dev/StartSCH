using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.BackgroundTasks;

[Index(nameof(Discriminator), nameof(WaitUntil), nameof(Created))]
public abstract class BackgroundTask
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? WaitUntil { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Discriminator { get; set; } = null!;
}

public abstract class SendNotification : BackgroundTask
{
    public User User { get; set; }
    public Notification Notification { get; set; }
}

public class EmailMessage
{
    public int Id { get; set; }
    [MaxLength(200)] public required string FromName { get; set; }
    [MaxLength(200)] public required string FromEmail { get; set; }
    [MaxLength(500)] public required string Subject { get; set; }
    [MaxLength(100_000)] public required string ContentHtml { get; set; }
}

public class SendEmail : BackgroundTask
{
    public int MessageId { get; set; }
    
    public User User { get; set; }
    public EmailMessage Message { get; set; }
}

public class PushNotificationMessage
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Url { get; set; }
}

public class SendPushNotification : BackgroundTask
{
    public int MessageId { get; set; }
    
    public User User { get; set; }
    public PushNotificationMessage Message { get; set; }
}

public class CreateEventStartedNotifications : BackgroundTask
{
    public Event Event { get; set; }
}

public class CreateOrderingStartedNotifications : BackgroundTask
{
    public PincerOpening PincerOpening { get; set; }
}

public class CreatePostPublishedNotifications : BackgroundTask
{
    public Post Post { get; set; }
}

using System.ComponentModel.DataAnnotations;
using Lib.Net.Http.WebPush;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

[Index(nameof(Discriminator), nameof(WaitUntil), nameof(Created))]
public abstract class BackgroundTask
{
    public int Id { get; init; }
    public required DateTime Created { get; init; }
    public DateTime? WaitUntil { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Discriminator { get; init; } = null!;

    public class DbConfiguration : IEntityTypeConfiguration<BackgroundTask>
    {
        public void Configure(EntityTypeBuilder<BackgroundTask> builder)
        {
            builder.HasDiscriminator(x => x.Discriminator);
        }
    }
}

public class CreateOrderingStartedNotifications : BackgroundTask
{
    public int PincerOpeningId { get; init; }
    public PincerOpening PincerOpening { get; init; } = null!;
}

public class CreatePostPublishedNotifications : BackgroundTask
{
    public int PostId { get; init; }
    public Post Post { get; init; } = null!;
}

public class SendEmail : BackgroundTask
{
    public int UserId { get; init; }
    public User User { get; init; } = null!;
    
    public int MessageId { get; init; }
    public EmailMessage Message { get; init; } = null!;
}

public class EmailMessage
{
    public int Id { get; init; }
    [MaxLength(200)] public required string FromName { get; init; }
    [MaxLength(200)] public string? FromEmail { get; init; }
    [MaxLength(500)] public required string Subject { get; init; }
    [MaxLength(100_000)] public required string ContentHtml { get; init; }
}

public class SendPushNotification : BackgroundTask
{
    public int UserId { get; init; }
    public User User { get; init; } = null!;

    public int MessageId { get; init; }
    public PushNotificationMessage Message { get; init; } = null!;
}

public class PushNotificationMessage
{
    public int Id { get; init; }
    [MaxLength(50_000)] public required string Payload { get; init; }
    [MaxLength(100)] public string? Topic { get; set; }
    public PushMessageUrgency? Urgency { get; set; }
    public DateTime? ValidUntil { get; set; }
}

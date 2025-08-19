using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

public partial class Db
{
    public DbSet<BackgroundTask> BackgroundTasks => Set<BackgroundTask>();

    public DbSet<CreateEventStartedNotifications> CreateEventStartedNotifications =>
        Set<CreateEventStartedNotifications>();

    public DbSet<CreateOrderingStartedNotifications> CreateOrderingStartedNotifications =>
        Set<CreateOrderingStartedNotifications>();

    public DbSet<CreatePostPublishedNotifications> CreatePostPublishedNotifications =>
        Set<CreatePostPublishedNotifications>();

    public DbSet<SendEmail> SendEmails => Set<SendEmail>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<SendPushNotification> SendPushNotifications => Set<SendPushNotification>();
    public DbSet<PushNotificationMessage> PushNotificationMessages => Set<PushNotificationMessage>();
}

[Index(nameof(Discriminator), nameof(WaitUntil), nameof(Created))]
public abstract class BackgroundTask
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? WaitUntil { get; set; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string Discriminator { get; set; } = null!;

    public class DbConfiguration : IEntityTypeConfiguration<BackgroundTask>
    {
        public void Configure(EntityTypeBuilder<BackgroundTask> builder)
        {
            builder.HasDiscriminator(x => x.Discriminator);
        }
    }
}

public class CreateEventStartedNotifications : BackgroundTask
{
    public Event Event { get; set; }
}

public class CreateOrderingStartedNotifications : BackgroundTask
{
    public int PincerOpeningId { get; set; }
    public PincerOpening PincerOpening { get; set; }
}

public class CreatePostPublishedNotifications : BackgroundTask
{
    public Post Post { get; set; }
}

public class SendEmail : BackgroundTask
{
    public int UserId { get; set; }
    public int MessageId { get; set; }

    public User User { get; set; }
    public EmailMessage Message { get; set; }
}

public class EmailMessage
{
    public int Id { get; set; }
    [MaxLength(200)] public required string FromName { get; set; }
    [MaxLength(200)] public required string FromEmail { get; set; }
    [MaxLength(500)] public required string Subject { get; set; }
    [MaxLength(100_000)] public required string ContentHtml { get; set; }
}

public class SendPushNotification : BackgroundTask
{
    public int UserId { get; set; }
    public int MessageId { get; set; }

    public User User { get; set; }
    public PushNotificationMessage Message { get; set; }
}

public class PushNotificationMessage
{
    public int Id { get; set; }
    [MaxLength(50_000)] public required string Payload { get; set; }
}

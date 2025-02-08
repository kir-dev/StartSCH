using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<EmailRequest> EmailRequests => Set<EmailRequest>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventNotification> EventNotifications => Set<EventNotification>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRequest> NotificationRequests => Set<NotificationRequest>();
    public DbSet<Opening> Openings => Set<Opening>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostNotification> PostNotifications => Set<PostNotification>();
    public DbSet<PushRequest> PushRequests => Set<PushRequest>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserTagSelection> UserTagSelections => Set<UserTagSelection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many#many-to-many-with-navigations-to-and-from-join-entity
        modelBuilder.Entity<User>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.SelectedBy)
            .UsingEntity<UserTagSelection>();
    }
}

public class User
{
    public Guid Id { get; init; } // PéK id

    [MaxLength(200)] public string? AuthSchEmail { get; set; } // only stored if verified
    [MaxLength(200)] public string? StartSchEmail { get; set; }
    public bool StartSchEmailVerified { get; set; }

    public List<Tag> Tags { get; } = [];
    public List<PushSubscription> PushSubscriptions { get; } = [];
}

public record Tag(string Path)
{
    public int Id { get; init; }

    public List<User> SelectedBy { get; } = [];
}

public class UserTagSelection
{
    public Guid UserId { get; init; }
    public int TagId { get; init; }

    public required User User { get; init; }
    public required Tag Tag { get; init; }
}

[Index(nameof(Url), IsUnique = true)]
public class Post
{
    public int Id { get; init; }
    public int? EventId { get; set; }

    [MaxLength(130)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? ExcerptMarkdown { get; set; }
    [MaxLength(50000)] public string? ContentMarkdown { get; set; }
    [MaxLength(500)] public string? Url { get; init; }
    public DateTime? PublishedUtc { get; set; }
    public required DateTime CreatedUtc { get; init; }

    public Event? Event { get; set; }
    public List<Group> Groups { get; } = [];
}

[Index(nameof(PekId), IsUnique = true)]
[Index(nameof(PincerName), IsUnique = true)]
public class Group
{
    public int Id { get; init; }
    public int? PekId { get; set; }
    [MaxLength(40)] public string? PekName { get; set; }
    [MaxLength(40)] public string? PincerName { get; set; }

    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
}

public class Opening : Event
{
    public DateTime? OrderingStartUtc { get; set; }
    public DateTime? OrderingEndUtc { get; set; }
}

public class Event
{
    public int Id { get; init; }
    public int? ParentId { get; set; }
    public required DateTime CreatedUtc { get; init; }
    public required DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    [MaxLength(130)] public required string Title { get; set; }
    [MaxLength(50000)] public string? DescriptionMarkdown { get; set; }

    public Event? Parent { get; set; }
    public List<Event> Children { get; } = [];
    public List<Group> Groups { get; } = [];
    public List<Post> Posts { get; } = [];
}

[Index(nameof(Endpoint), IsUnique = true)]
public class PushSubscription
{
    public int Id { get; init; }

    public Guid UserId { get; set; }

    // max lengths are arbitrary, may need to be adjusted
    [MaxLength(2000)] public required string Endpoint { get; init; }
    [MaxLength(100)] public required string P256DH { get; init; }
    [MaxLength(50)] public required string Auth { get; init; }

    public User User { get; set; } = null!;
}

// NOTIFICATION QUEUE //

public class EventNotification : Notification
{
    public required Event Event { get; init; }
}

public class PostNotification : Notification
{
    public required Post Post { get; init; }
}

public abstract class Notification
{
    public int Id { get; init; }

    public List<NotificationRequest> Requests { get; } = [];
}

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

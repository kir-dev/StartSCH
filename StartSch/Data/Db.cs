using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Opening> Openings => Set<Opening>();
    public DbSet<Post> Posts => Set<Post>();
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
    public Guid Id { get; init; } // pék id

    public List<Tag> Tags { get; } = [];
    public List<PushSubscription> PushSubscriptions { get; } = [];
}

public record Tag(string Path)
{
    public int Id { get; init; }

    public List<Post> Posts { get; } = [];
    public List<Event> Events { get; } = [];
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
    [MaxLength(130)] public required string Title { get; set; }
    [MaxLength(1000)] public string? ExcerptMarkdown { get; set; }
    [MaxLength(50000)] public string? ContentMarkdown { get; set; }
    [MaxLength(500)] public string? Url { get; init; }
    public DateTime? PublishedUtc { get; set; }

    public List<Tag> Tags { get; } = [];
    public List<Group> Groups { get; } = [];
    public Event? Event { get; init; }
}

[Index(nameof(PekId), IsUnique = true)]
[Index(nameof(PincerName), IsUnique = true)]
public class Group
{
    public int Id { get; init; }
    public int? PekId { get; set; }
    [MaxLength(40)] public string? PekName { get; set; }
    [MaxLength(40)] public string? PincerName { get; set; }

    public List<Post> Posts { get; } = [];
    public List<Event> Events { get; } = [];
}

public class Opening
{
    public int Id { get; init; }
    public int EventId { get; init; }

    public Event Event { get; init; } = null!;
}

public class Event
{
    public int Id { get; init; }
    public required DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public required DateTime CreatedUtc { get; init; }
    [MaxLength(255)] public required string Title { get; set; }

    public List<Group> Groups { get; } = [];
    public Opening? Opening { get; init; }
    public List<Post> Posts { get; } = [];
    public List<Tag> Tags { get; } = [];
}

[Index(nameof(Endpoint), IsUnique = true)]
public class PushSubscription
{
    public int Id { get; init; }

    public Guid UserId { get; init; }

    // max lengths are arbitrary, may need to be adjusted
    [MaxLength(2000)] public required string Endpoint { get; init; }
    [MaxLength(100)] public required string P256DH { get; init; }
    [MaxLength(50)] public required string Auth { get; init; }

    public User User { get; init; } = null!;
}
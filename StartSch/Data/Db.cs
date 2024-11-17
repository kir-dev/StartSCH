using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
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
    public Guid Id { get; set; } // pék id

    public List<Tag> Tags { get; set; } = [];
    public List<PushSubscription> PushSubscriptions { get; set; } = [];
}

public record Tag(string Path)
{
    public int Id { get; set; }

    public List<Post> Posts { get; set; } = [];
    public List<Opening> Openings { get; set; } = [];
    public List<Event> Events { get; set; } = [];
    public List<User> SelectedBy { get; set; } = [];
}

public class UserTagSelection
{
    public Guid UserId { get; set; }
    public int TagId { get; set; }

    public User User { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

public class Post
{
    public int Id { get; set; }
    [MaxLength(50)] public required string Title { get; set; }
    [MaxLength(300)] public string? Excerpt { get; set; }
    [MaxLength(2000)] public string? Body { get; set; }
    [MaxLength(500)] public string? Url { get; set; }
    public DateTime PublishedAtUtc { get; set; }

    public List<Tag> Tags { get; set; } = [];
}

[Index(nameof(PekId), IsUnique = true)]
[Index(nameof(PincerName), IsUnique = true)]
public class Group
{
    public int Id { get; set; }
    public int? PekId { get; set; }
    [MaxLength(40)] public string? PekName { get; set; }
    [MaxLength(40)] public string? PincerName { get; set; }

    public List<Post> Posts { get; set; } = [];
    public List<Opening> Openings { get; set; } = [];
}

public class Opening
{
    public int Id { get; set; }
    public DateTime StartUtc { get; set; }
    [MaxLength(255)] public string Title { get; set; } = null!;

    public Group Group { get; set; } = null!;
    public List<Post> Posts { get; set; } = [];
}

public class Event
{
    public int Id { get; set; }

    public Group? Group { get; set; }
    public List<Post> Posts { get; set; } = [];
}

[Index(nameof(Endpoint), IsUnique = true)]
public class PushSubscription
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    // max lengths are arbitrary, may need to be adjusted
    [MaxLength(2000)] public string Endpoint { get; set; } = null!;
    [MaxLength(100)] public string P256DH { get; set; } = null!;
    [MaxLength(50)] public string Auth { get; set; } = null!;

    public User User { get; set; } = null!;
}
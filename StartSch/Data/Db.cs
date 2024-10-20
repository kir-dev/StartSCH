using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many#unidirectional-many-to-many
        modelBuilder.Entity<Post>()
            .HasMany(p => p.Tags)
            .WithMany();
    }
}

public record Tag(string Path)
{
    public int Id { get; set; }
}

public class Post
{
    public int Id { get; set; }
    [MaxLength(50)] public required string Title { get; set; }
    [MaxLength(300)] public string? Excerpt { get; set; }
    [MaxLength(2000)] public string? Body { get; set; }
    [MaxLength(500)] public string? Url { get; set; }
    public DateTime PublishedAtUtc { get; set; }
    public ICollection<Tag> Tags { get; set; } = [];
}

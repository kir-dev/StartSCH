using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

[Index(nameof(ExternalUrl), IsUnique = true)]
public class Post : IAutoCreatedUpdated
{
    public int Id { get; init; }
    public int? EventId { get; set; }

    [MaxLength(300)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? ExcerptMarkdown { get; set; }
    [MaxLength(200_000)] public string? ContentMarkdown { get; set; }
    [MaxLength(1000)] public string? ExternalUrl { get; set; }
    public int? ExternalIdInt { get; init; }

    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public DateTime? Published { get; set; }
    
    public List<Category> Categories { get; } = [];
    public List<PostCategory> PostCategories { get; } = [];
    public Event? Event { get; set; }
    
    public class DbConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder
                .HasMany(p => p.Categories)
                .WithMany(c => c.Posts)
                .UsingEntity<PostCategory>();
        }
    }
}

public class PostCategory
{
    public int PostId { get; init; }
    public int CategoryId { get; init; }
}

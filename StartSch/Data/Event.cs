using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

[Index(nameof(ParentId), nameof(ExternalIdInt), IsUnique = true)]
public class Event : IAutoCreatedUpdated
{
    public int Id { get; init; }
    public int? ParentId { get; set; }
    
    public Instant Created { get; set; }
    public Instant Updated { get; set; }
    public Instant? Start { get; set; }
    public Instant? End { get; set; }
    public bool AllDay { get; set; }
    [MaxLength(300)] public required string Title { get; set; }
    [MaxLength(50000)] public string? DescriptionMarkdown { get; set; }
    [MaxLength(1000)] public string? ExternalUrl { get; set; }
    public int? ExternalIdInt { get; init; }
    
    public List<Category> Categories { get; } = [];
    public List<EventCategory> EventCategories { get; } = [];
    public Event? Parent { get; set; }

    public List<Event> Children { get; } = [];
    public List<Post> Posts { get; } = [];
    
    public class DbConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder
                .HasMany(e => e.Categories)
                .WithMany(c => c.Events)
                .UsingEntity<EventCategory>();
        }
    }
}

public class EventCategory
{
    public int EventId { get; init; }
    public int CategoryId { get; init; }
}

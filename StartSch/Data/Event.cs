using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

public class Event : IAutoCreatedUpdated
{
    public int Id { get; init; }
    public int? ParentId { get; set; }
    
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    [MaxLength(300)] public required string Title { get; set; }
    [MaxLength(50000)] public string? DescriptionMarkdown { get; set; }
    [MaxLength(1000)] public string? Url { get; init; }
    
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

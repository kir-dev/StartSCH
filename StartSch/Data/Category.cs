using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

[Index(nameof(OwnerId))]
public class Category
{
    public int Id { get; init; }
    public int OwnerId { get; init; }
    
    [MaxLength(30)] public string? Name { get; set; } // TODO: Add category identifiers
    
    public required Page Owner { get; set; }

    public List<Category> IncludedCategories { get; } = [];
    public List<Category> IncludedBy { get; } = [];

    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
    
    public class DbConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            var indexBuilder = builder.HasIndex(nameof(OwnerId), nameof(Name))
                .IsUnique();
            indexBuilder.AreNullsDistinct(false);
        }
    }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

[Index(nameof(CodeIdentifier), IsUnique = true)]
[Index(nameof(OwnerId))]
public class Category
{
    public int Id { get; init; }
    public int OwnerId { get; init; }
    
    [MaxLength(100)] public string? CodeIdentifier { get; set; }
    [MaxLength(30)] public string? Name { get; set; } // TODO: Add category identifiers
    
    public required Page Owner { get; set; }

    public List<Category> IncludedCategories { get; } = [];
    public List<Category> IncluderCategories { get; } = [];

    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
    
    public class DbConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder
                .HasIndex(nameof(OwnerId), nameof(Name))
                .IsUnique()
                .AreNullsDistinct(false);
            builder
                .HasMany<Category>(c => c.IncluderCategories)
                .WithMany(c => c.IncludedCategories)
                .UsingEntity<CategoryInclude>();
        }
    }
}

public class CategoryInclude
{
    public required Category Includer { get; init; }
    public required Category Included { get; init; }
}

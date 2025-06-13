using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

[Index(nameof(PageId))]
public class Category
{
    public int Id { get; init; }
    public int PageId { get; init; }
    
    [MaxLength(100)] public string? Name { get; set; }
    
    public required Page Page { get; init; }

    public List<Category> IncludedCategories { get; } = [];
    public List<Category> IncluderCategories { get; } = [];

    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
    public List<CategoryInterest> Interests { get; } = [];
    
    public class DbConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder
                .HasIndex(nameof(PageId), nameof(Name))
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

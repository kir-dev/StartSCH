using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StartSch.Data;

[Index(nameof(PageId), nameof(Name))]
[Index(nameof(PageId), nameof(ExternalIdInt), IsUnique = true)]
public class Category
{
    public int Id { get; init; }
    public int PageId { get; init; }
    
    [MaxLength(100)] public string? Name { get; set; }
    [MaxLength(1000)] public string? ExternalUrl { get; set; }
    [MaxLength(100)] public int? ExternalIdInt { get; init; }

    public Page Page { get; init; } = null!;

    public List<Category> IncludedCategories { get; } = [];
    public List<Category> IncluderCategories { get; } = [];

    public List<CategoryInclude> IncludedCategoryIncludes { get; } = [];
    public List<CategoryInclude> IncluderCategoryIncludes { get; } = [];

    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
    public List<CategoryInterest> Interests { get; } = [];
    
    public class DbConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> category)
        {
            // https://learn.microsoft.com/en-us/ef/core/modeling/relationships/many-to-many#many-to-many-with-navigations-to-and-from-join-entity
            category
                .HasMany(c => c.IncludedCategories)
                .WithMany(c => c.IncluderCategories)
                .UsingEntity<CategoryInclude>(
                    include => include.HasOne(i => i.Included).WithMany(c => c.IncluderCategoryIncludes),
                    include => include.HasOne(i => i.Includer).WithMany(c => c.IncludedCategoryIncludes)
                );
        }
    }
}

public class CategoryInclude
{
    public int IncluderId { get; init; }
    public int IncludedId { get; init; }
    public Category Includer { get; init; } = null!;
    public Category Included { get; init; } = null!;
}

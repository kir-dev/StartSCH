using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(PekId), IsUnique = true)]
[Index(nameof(PekName), IsUnique = true)]
[Index(nameof(PincerId), IsUnique = true)]
[Index(nameof(PincerName), IsUnique = true)]
[Index(nameof(ExternalUrl), IsUnique = true)]
public class Page : ICreatedUpdated
{
    public int Id { get; init; }
    
    public Instant Created { get; set; }
    public Instant Updated { get; set; }
    
    [MaxLength(100)] public string? ExternalUrl { get; set; }
    [MaxLength(100)] public string? Name { get; set; }

    [NotNullIfNotNull(nameof(PekName))] public int? PekId { get; set; }

    [NotNullIfNotNull(nameof(PekId)), MaxLength(200)]
    public string? PekName { get; set; }

    [NotNullIfNotNull(nameof(PincerName))] public int? PincerId { get; set; }

    [NotNullIfNotNull(nameof(PincerId)), MaxLength(200)]
    public string? PincerName { get; set; }

    public List<Category> Categories { get; } = [];
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(PekId), IsUnique = true)]
[Index(nameof(PekName), IsUnique = true)]
[Index(nameof(PincerId), IsUnique = true)]
[Index(nameof(PincerName), IsUnique = true)]
public class Page
{
    public int Id { get; init; }

    [NotNullIfNotNull(nameof(PekName))] public int? PekId { get; set; }

    [NotNullIfNotNull(nameof(PekId)), MaxLength(40)]
    public string? PekName { get; set; }

    [NotNullIfNotNull(nameof(PincerName))] public int? PincerId { get; set; }

    [NotNullIfNotNull(nameof(PincerId)), MaxLength(40)]
    public string? PincerName { get; set; }

    public List<Category> Categories { get; } = [];
}

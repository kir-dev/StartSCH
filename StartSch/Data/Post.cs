using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(Url), IsUnique = true)]
public class Post
{
    public int Id { get; init; }
    public int? EventId { get; set; }

    [MaxLength(130)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? ExcerptMarkdown { get; set; }
    [MaxLength(50000)] public string? ContentMarkdown { get; set; }
    [MaxLength(500)] public string? Url { get; init; }
    public DateTime? PublishedUtc { get; set; }
    public required DateTime CreatedUtc { get; init; }
    
    public List<Category> Categories { get; } = [];
    public Event? Event { get; set; }
}

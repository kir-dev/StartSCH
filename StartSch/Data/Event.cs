using System.ComponentModel.DataAnnotations;

namespace StartSch.Data;

public class Event
{
    public int Id { get; init; }
    public int? ParentId { get; set; }
    
    public required DateTime CreatedUtc { get; init; }
    public required DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    [MaxLength(130)] public required string Title { get; set; }
    [MaxLength(50000)] public string? DescriptionMarkdown { get; set; }
    
    public List<Category> Categories { get; } = [];
    public Event? Parent { get; set; }

    public List<Event> Children { get; } = [];
}

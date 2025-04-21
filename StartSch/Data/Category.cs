namespace StartSch.Data;

public class Category
{
    public int Id { get; init; }
    
    public required Page Owner { get; set; }

    public List<Category> IncludedCategories { get; } = [];
    public List<Category> IncludedBy { get; } = [];

    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
}

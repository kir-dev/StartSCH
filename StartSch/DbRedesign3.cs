// ReSharper disable once CheckNamespace
namespace StartSch.DbRedesign3;

// Page:
//     Categories
// 
// Category:
//     Feed
//     IncludedCategories
//     Events
//     Posts
// 
// Events:
//     Categories
//     Parent
//     Children
// 
// Posts:
//     Categories

public class Page
{
    public List<Category> Categories { get; } = [];
}

// pages always have a default category. it usually includes all other categories (not necessary for aggregates).
public class Category
{
    public Page Owner { get; set; } = null!;
    
    public List<Category> IncludedCategories { get; } = [];
    public List<Category> IncludedBy { get; } = [];
    
    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
}

public class Event
{
    public List<Category> Categories { get; } = [];
    public Event? Parent { get; set; }
    
    public List<Event> Children { get; } = [];
}

public class Post
{
    public List<Category> Categories { get; } = [];
    public Event? Event { get; set; }
}

// --------------------------------------------

public class OrderingStartInterest : Interest
{
    // public required Opening Opening { get; init; }
}

public class CategoryInterest : Interest
{
    public required Category Category { get; init; }
}

public class EventInterest : Interest
{
    public required Event Event { get; init; }
}

public abstract class Interest
{
}

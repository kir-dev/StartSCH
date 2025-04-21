// ReSharper disable once CheckNamespace
namespace StartSch.DbRedesign;

// schpincer is a feed with its own categories
// pincer groups also have their own categories
public class Feed
{
    public List<Category> Categories { get; } = [];
}

// every event/post must belong to a category
// feeds with only a single category hide it from the UI
// events/posts can belong to categories from different feeds
public class Category
{
    public Feed Feed { get; set; } = null!;
    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
}

public class Event
{
    public List<Category> Categories { get; } = [];
}

public class Post
{
    public List<Category> Categories { get; } = [];
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

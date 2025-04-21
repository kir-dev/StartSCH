// ReSharper disable once CheckNamespace
namespace StartSch.DbRedesign2;

// schpincer is a feed with its own categories
// pincer groups are also feeds with their own categories
public class Feed
{
    public List<Category> Categories { get; } = [];
    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
}

// every event/post must belong to either a category or a feed
// events/posts can belong to categories from different feeds
public class Category
{
    public Feed Feed { get; set; } = null!;
    
    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
}

// all events/posts must belong to at least one feed
// all feeds are considered owners
// schpincer can aggregate events/posts through its categories but can't own any of them
public class Event
{
    public List<Feed> Feeds { get; } = [];
    public List<Category> Categories { get; } = [];
}

public class Post
{
    public List<Feed> Feeds { get; } = [];
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

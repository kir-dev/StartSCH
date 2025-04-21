namespace StartSch.Data;

public partial class Page
{
    public List<Category> Categories { get; } = [];
}

public partial class Category
{
    public required Page Owner { get; set; }

    public List<Category> IncludedCategories { get; } = [];
    public List<Category> IncludedBy { get; } = [];

    public List<Event> Events { get; } = [];
    public List<Post> Posts { get; } = [];
}

public partial class Event
{
    public List<Category> Categories { get; } = [];
    public Event? Parent { get; set; }

    public List<Event> Children { get; } = [];
}

public partial class Post
{
    public List<Category> Categories { get; } = [];
    public Event? Event { get; set; }
}

public partial class User
{
    public List<InterestSubscription> InterestSubscriptions { get; } = [];
    public List<PushSubscription> PushSubscriptions { get; } = [];
}

public abstract partial class InterestSubscription
{
    public User User { get; set; }
    public Interest Interest { get; set; }
}

public class PushInterestSubscription : InterestSubscription
{
}

public class EmailInterestSubscription : InterestSubscription
{
}

public class HomepageInterestSubscription : InterestSubscription
{
}

public partial class Interest
{
    public List<InterestSubscription> Subscriptions { get; } = [];
}

public class OrderingStartInterest : Interest
{
    public required Opening Opening { get; init; }
}

public class CategoryInterest : Interest
{
    public required Category Category { get; init; }
}

public class EventInterest : Interest
{
    public required Event Event { get; init; }
}

public partial class PushSubscription
{
    public User User { get; set; } = null!;
}

public abstract class Notification
{
    public int Id { get; init; }

    public List<NotificationRequest> Requests { get; } = [];
}

public class OrderingStartedNotification : Notification
{
    public required Opening Opening { get; init; }
}

public class PostNotification : Notification
{
    public required Post Post { get; init; }
}

public abstract class NotificationRequest
{
    public int Id { get; init; }

    public required DateTime CreatedUtc { get; init; }

    public Notification Notification { get; init; } = null!;
    public required User User { get; init; }
}

public class PushRequest : NotificationRequest
{
}

public class EmailRequest : NotificationRequest
{
}

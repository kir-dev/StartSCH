namespace StartSch.Data;

public abstract class Interest
{
    public int Id { get; set; }

    public List<User> Subscribers { get; } = [];
    public List<InterestSubscription> Subscriptions { get; } = [];
}

public abstract class CategoryInterest : Interest
{
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public abstract class EventInterest : Interest
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;
}

public class ShowEventsInCategory : CategoryInterest;
public class ShowPostsForEvent : EventInterest;
public class ShowPostsInCategory : CategoryInterest;

public class EmailWhenOrderingStartedInCategory : CategoryInterest;
public class EmailWhenPostPublishedForEvent : EventInterest;
public class EmailWhenPostPublishedInCategory : CategoryInterest;

public class PushWhenOrderingStartedInCategory : CategoryInterest;
public class PushWhenPostPublishedForEvent : EventInterest;
public class PushWhenPostPublishedInCategory : CategoryInterest;

public static class InterestQueryableExtensions
{
    public static IQueryable<InterestSubscription> WherePushInterestSubscription(this IQueryable<InterestSubscription> query) => query
        .Where(s =>
            s.Interest is PushWhenOrderingStartedInCategory
            || s.Interest is PushWhenPostPublishedForEvent
            || s.Interest is PushWhenPostPublishedInCategory);
}

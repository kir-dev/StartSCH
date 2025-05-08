namespace StartSch.Data;

public abstract class Interest
{
    public int Id { get; init; }

    public List<InterestSubscription> Subscriptions { get; } = [];
}

public class CategoryInterest : Interest
{
    public required Category Category { get; init; }
}

public class EventInterest : Interest
{
    public required Event Event { get; init; }
}

public class OrderingStartInterest : Interest
{
    public required Category Category { get; init; }
}

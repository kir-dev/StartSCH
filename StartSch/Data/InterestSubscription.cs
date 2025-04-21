namespace StartSch.Data;

public abstract class InterestSubscription
{
    public int Id { get; init; }

    public required User User { get; init; }
    public required Interest Interest { get; init; }
}

public class EmailInterestSubscription : InterestSubscription
{
}

public class HomepageInterestSubscription : InterestSubscription
{
}

public class PushInterestSubscription : InterestSubscription
{
}

namespace StartSch.Data;

public abstract class InterestSubscription
{
    public int Id { get; init; }

    public User User { get; set; }
    public Interest Interest { get; set; }
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

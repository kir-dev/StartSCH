using JetBrains.Annotations;

namespace StartSch.Data;

public abstract class InterestSubscription
{
    public int Id { get; init; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int UserId { get; init; }

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

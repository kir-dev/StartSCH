using JetBrains.Annotations;

namespace StartSch.Data;

public class InterestSubscription
{
    public int Id { get; init; }

    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int UserId { get; init; }
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public int InterestId { get; init; }

    public User User { get; init; } = null!;
    public Interest Interest { get; init; } = null!;
}

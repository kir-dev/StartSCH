using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(UserId), nameof(InterestId))] // this may or may not improve performance. couldn't figure out why ef doesn't do this by default
public class InterestSubscription
{
    public int InterestId { get; init; }
    public int UserId { get; init; }
    public Interest Interest { get; init; } = null!;
    public User User { get; init; } = null!;
}

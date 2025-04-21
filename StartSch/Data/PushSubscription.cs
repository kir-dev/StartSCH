using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(Endpoint), IsUnique = true)]
public class PushSubscription
{
    public int Id { get; init; }

    public Guid UserId { get; set; }

    // max lengths are arbitrary, may need to be adjusted
    [MaxLength(2000)] public required string Endpoint { get; init; }
    [MaxLength(100)] public required string P256DH { get; init; }
    [MaxLength(50)] public required string Auth { get; init; }
    
    public User User { get; set; } = null!;
}

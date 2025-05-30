using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(AuthSchId), IsUnique = true)]
public class User
{
    public int Id { get; init; }
    public Guid? AuthSchId { get; init; }

    [MaxLength(200)] public string? AuthSchEmail { get; set; } // only stored if verified
    [MaxLength(200)] public string? StartSchEmail { get; set; }
    public bool StartSchEmailVerified { get; set; }
    
    public List<Interest> Interests { get; } = [];
    public List<InterestSubscription> InterestSubscriptions { get; } = [];
    public List<PushSubscription> PushSubscriptions { get; } = [];
}

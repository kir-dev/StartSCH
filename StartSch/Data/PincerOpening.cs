using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(PincerId), IsUnique = true)]
public class PincerOpening : Event
{
    public required int PincerId { get; init; }
    public Instant? OrderingStart { get; set; }
    public Instant? OrderingEnd { get; set; }
    public Instant? OutOfStock { get; set; }
    
    public CreateOrderingStartedNotifications? CreateOrderingStartedNotifications { get; set; }
}

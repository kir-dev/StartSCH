using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(PincerId), IsUnique = true)]
public class PincerOpening : Event
{
    public required int PincerId { get; init; }
    public DateTime? OrderingStartUtc { get; set; }
    public DateTime? OrderingEndUtc { get; set; }
    public DateTime? OutOfStockUtc { get; set; }
}

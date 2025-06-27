using Microsoft.EntityFrameworkCore;

namespace StartSch.Data;

[Index(nameof(PincerId), IsUnique = true)]
public class PincerOpening : Event
{
    public required int PincerId { get; init; }
    public DateTime? OrderingStart { get; set; }
    public DateTime? OrderingEnd { get; set; }
    public DateTime? OutOfStock { get; set; }
}

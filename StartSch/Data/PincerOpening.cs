namespace StartSch.Data;

public class PincerOpening : Event
{
    public required int PincerId { get; init; }
    public DateTime? OrderingStartUtc { get; set; }
    public DateTime? OrderingEndUtc { get; set; }
    public DateTime? OutOfStockUtc { get; set; }
}

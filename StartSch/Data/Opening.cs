namespace StartSch.Data;

public class Opening : Event
{
    public DateTime? OrderingStartUtc { get; set; }
    public DateTime? OrderingEndUtc { get; set; }
    public DateTime? OutOfStockUtc { get; set; }
}
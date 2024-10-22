namespace StartSch.Modules.SchPincer;

public class Opening(string group, string title, DateTime? ordersStartUtc, DateTime? startUtc, DateTime? endUtc)
{
    public int GroupId { get; set; } = 0;
    public string Group { get; set; } = group;
    public string? Title { get; set; } = title;
    public DateTime? OrdersStartUtc { get; set; } = ordersStartUtc;
    public DateTime? OrdersEndUtc { get; set; }
    public DateTime? StartUtc { get; set; } = startUtc;
    public DateTime? EndUt { get; set; } = endUtc;
}
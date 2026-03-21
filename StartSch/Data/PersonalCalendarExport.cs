namespace StartSch.Data;

public class PersonalCalendarExport
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }

    public User User { get; set; } = null!;
}

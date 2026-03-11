namespace StartSch.Data;

public class PersonalCalendar
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required User User { get; set; }
}

public class ExternalPersonalCalendar : PersonalCalendar
{
    public required byte[] EncryptedUrl { get; set; }
}

public class PersonalMoodleCalendar : ExternalPersonalCalendar;

public class PersonalNeptunCalendar : ExternalPersonalCalendar;

public class PersonalStartSchCalendar : PersonalCalendar
{
    public List<Event> Events { get; set; }
}

public class PersonalCalendarExport
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
}



using System.Text.Json.Serialization;

namespace StartSch.Wasm;

[JsonDerivedType(typeof(PersonalStartSchCalendarLive), nameof(PersonalStartSchCalendarLive))]
[JsonDerivedType(typeof(PersonalNeptunCalendarLive), nameof(PersonalNeptunCalendarLive))]
[JsonDerivedType(typeof(PersonalMoodleCalendarLive), nameof(PersonalMoodleCalendarLive))]
public abstract class PersonalCalendarLive
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<PersonalCalendarEvent>? Events { get; set; }
}

public class PersonalStartSchCalendarLive : PersonalCalendarLive;

public abstract class ExternalPersonalCalendarLive : PersonalCalendarLive
{
    public string Url { get; set; } = "";
}

public class PersonalNeptunCalendarLive : ExternalPersonalCalendarLive;

public class PersonalMoodleCalendarLive : ExternalPersonalCalendarLive;

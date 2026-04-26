using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace StartSch.Wasm;

[JsonDerivedType(typeof(PersonalStartSchCalendarLive), nameof(PersonalStartSchCalendarLive))]
[JsonDerivedType(typeof(PersonalNeptunCalendarLive), nameof(PersonalNeptunCalendarLive))]
[JsonDerivedType(typeof(PersonalMoodleCalendarLive), nameof(PersonalMoodleCalendarLive))]
public abstract class PersonalCalendarLive
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<PersonalCalendarEvent> Events { get; [UsedImplicitly] set; } = [];
}

public class PersonalStartSchCalendarLive : PersonalCalendarLive
{
    public string? IcsUrl { get; set; }
}

public abstract class ExternalPersonalCalendarLive : PersonalCalendarLive
{
    public string Url { get; set; } = "";
}

public class PersonalNeptunCalendarLive : ExternalPersonalCalendarLive;

public class PersonalMoodleCalendarLive : ExternalPersonalCalendarLive;

namespace StartSch.Wasm;

public class PersonalCalendarLive
{
}

public class PersonalStartSchCalendarLive : PersonalCalendarLive
{
}

public class ExternalPersonalCalendarLive : PersonalCalendarLive
{
    public string Url { get; set; } = "";
}

public class PersonalNeptunCalendarLive : ExternalPersonalCalendarLive
{
}

public class PersonalMoodleCalendarLive : ExternalPersonalCalendarLive
{
}

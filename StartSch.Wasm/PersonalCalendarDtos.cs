namespace StartSch.Wasm;

public record PersonalCalendarsResult(
    List<PersonalStartSchCalendarLive> StartSchCalendars,
    List<PersonalNeptunCalendarLive> NeptunCalendars,
    List<PersonalMoodleCalendarLive> MoodleCalendars
);

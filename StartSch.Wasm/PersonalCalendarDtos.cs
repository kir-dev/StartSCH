namespace StartSch.Wasm;

public record PersonalCalendarsResult(
    List<PersonalCalendarCategoryLive> StartSchCalendars,
    List<PersonalNeptunCalendarLive> NeptunCalendars,
    List<PersonalMoodleCalendarLive> MoodleCalendars
);

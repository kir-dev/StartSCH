using System.Text.Json.Serialization;
using NodaTime;

namespace StartSch.Wasm.PersonalCalendars;

public class PersonalCalendarEvent
{
    public required string Id { get; set; }
    public int? CategoryCalendarId { get; set; }
    public required string Title { get; set; }

    [JsonConverter(typeof(InstantJsonConverter))]
    public Instant Start { get; set; }

    [JsonConverter(typeof(InstantJsonConverter))]
    public Instant End { get; set; }

    public PersonalCalendarEventSpecialType? SpecialType { get; set; }
    public string? Location { get; set; }
    public string? SubjectId { get; set; }
    public string? Subject { get; set; }
    public string? Course { get; set; }
    public List<string>? Teachers { get; set; }

    [JsonIgnore] public PersonalCalendarLive SourceCalendar { get; set; } = null!;
    [JsonIgnore] public PersonalCalendarCategoryLive? CategoryCalendarOrDefault { get => field ?? GetDefaultCategory?.Invoke(); set; }
    [JsonIgnore] public Func<PersonalCalendarCategoryLive>? GetDefaultCategory { get; set; }

    public PersonalCalendarEvent Copy()
    {
        return new()
        {
            Id = Id,
            CategoryCalendarId = CategoryCalendarId,
            Title = Title,
            Start = Start,
            End = End,
            SpecialType = SpecialType,
            Location = Location,
            SubjectId = SubjectId,
            Subject = Subject,
            Course = Course,
            Teachers = Teachers?.ToList(),
            SourceCalendar = SourceCalendar,
            CategoryCalendarOrDefault = CategoryCalendarOrDefault,
            GetDefaultCategory = GetDefaultCategory,
        };
    }
}

public enum PersonalCalendarEventSpecialType
{
    Final,
}

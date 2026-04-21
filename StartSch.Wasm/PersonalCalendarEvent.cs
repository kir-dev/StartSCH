using System.Text.Json.Serialization;
using NodaTime;

namespace StartSch.Wasm;

public class PersonalCalendarEvent
{
    public required string Id { get; set; }
    public int SourceCalendarId { get; set; }
    public int? CategoryCalendarId { get; set; }
    public required string Title { get; set; }

    [JsonConverter(typeof(InstantJsonConverter))]
    public required Instant Start { get; set; }

    [JsonConverter(typeof(InstantJsonConverter))]
    public required Instant End { get; set; }

    public string? Location { get; set; }
    public string? Subject { get; set; }
    public string? Course { get; set; }
    public List<string>? Teachers { get; set; }
    
    [JsonIgnore]
    public PersonalCalendarLive? CategoryCalendar { get; set; }

    public PersonalCalendarEvent Copy()
    {
        return new()
        {
            Id = Id,
            SourceCalendarId = SourceCalendarId,
            CategoryCalendarId = CategoryCalendarId,
            Title = Title,
            Start = Start,
            End = End,
            Location = Location,
            Subject = Subject,
            Course = Course,
            Teachers = Teachers?.ToList(),
        };
    }
}

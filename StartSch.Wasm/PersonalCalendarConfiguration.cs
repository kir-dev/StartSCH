using NodaTime;

namespace StartSch.Wasm;

public class PersonalCalendarConfiguration
{
    public PersonalCalendarConfiguration(PersonalCalendarConfigurationDto dto)
    {
        foreach (var modification in dto.Modifications)
            NeptunConfiguration.Modifications[modification.SubjectAndCourse] = modification;
    }

    public NeptunConfiguration NeptunConfiguration { get; } = new();

    public PersonalCalendarConfigurationDto ToDto()
    {
        return new()
        {
            Modifications = NeptunConfiguration.Modifications.Values.ToList(),
        };
    }
}

public class NeptunConfiguration
{
    public Dictionary<NeptunSubjectAndCourse, Modification> Modifications { get; } = [];
}

public record struct NeptunSubjectAndCourse(string Subject, string Course);

public class Modification
{
    public required NeptunSubjectAndCourse SubjectAndCourse { get; set; }
    public required List<Instant> Dates { get; set; }
    public int? NewCategoryId { get; set; }
}

using System.Globalization;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MaterialColorUtilities.Utils;

namespace StartSch.Wasm.PersonalCalendars;

[JsonDerivedType(typeof(PersonalCalendarCategoryLive), nameof(PersonalCalendarCategoryLive))]
[JsonDerivedType(typeof(PersonalNeptunCalendarLive), nameof(PersonalNeptunCalendarLive))]
[JsonDerivedType(typeof(PersonalMoodleCalendarLive), nameof(PersonalMoodleCalendarLive))]
public abstract class PersonalCalendarLive
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<PersonalCalendarEvent> Events { get; [UsedImplicitly] set; } = [];
}

public class PersonalCalendarCategoryLive : PersonalCalendarLive
{
    public string? IcsUrl { get; set; }

    public required string Color
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            double tone = ColorUtils.LStarFromArgb(
                uint.Parse(value.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture));
            TextColor = tone > 47.5 ? "#000000" : "#ffffff";
        }
    }

    [JsonIgnore] public string TextColor { get; private set; } = null!;
}

public abstract class ExternalPersonalCalendarLive : PersonalCalendarLive
{
    public string Url { get; set; } = "";
}

public class PersonalNeptunCalendarLive : ExternalPersonalCalendarLive;

public class PersonalMoodleCalendarLive : ExternalPersonalCalendarLive;

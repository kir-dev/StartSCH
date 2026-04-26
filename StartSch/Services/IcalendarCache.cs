using Ical.Net;
using Microsoft.Extensions.Caching.Memory;
using NodaTime.Extensions;
using IcalendarEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace StartSch.Services;

public class IcalendarCache(
    IMemoryCache memoryCache,
    HttpClient httpClient
)
{
    public async Task<List<PersonalCalendarEvent>> GetEvents(string url, Type externalCalendarType)
    {
        return await memoryCache.GetOrCreateAsync(
            $"ical {externalCalendarType.Name} {url}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                string s = await httpClient.GetStringAsync(url);
                var cal = Calendar.Load(s);
                var res = cal.Events
                    .Select(icalendarEvent => GetPersonalCalendarEvent(icalendarEvent, externalCalendarType))
                    .ToList();
                return res;
            });
    }

    private static PersonalCalendarEvent? GetPersonalCalendarEvent(IcalendarEvent icalendarEvent, Type externalCalendarType)
    {
        if (icalendarEvent.Uid is not { } id) return null;
        if (icalendarEvent.Start is not { AsUtc: var startDateTime }) return null;
        if (icalendarEvent.End is not { AsUtc: var endDateTime }) return null;
        if (icalendarEvent.Summary is not { } summary) return null;
        
        NeptunLessonEventTitleData? neptunLessonEventTitleData = null;
        NeptunFinalEventTitleData? neptunFinalEventTitleData = null;
        if (externalCalendarType == typeof(PersonalNeptunCalendarLive))
        {
            TryParseNeptunLessonTitle(icalendarEvent.Summary!, out neptunLessonEventTitleData);
            TryParseNeptunFinalTitle(icalendarEvent.Summary!, out neptunFinalEventTitleData);
        }

        return new()
        {
            Id = id,
            Start = startDateTime.ToInstant(),
            End = endDateTime.ToInstant(),
            Title = neptunLessonEventTitleData != null
                    ? $"{neptunLessonEventTitleData.Value.Subject} {neptunLessonEventTitleData.Value.Course}"
                    : neptunFinalEventTitleData != null
                        ? $"{neptunFinalEventTitleData.Value.Subject} vizsga ({neptunFinalEventTitleData.Value.Kind})"
                        : summary,
            SpecialType = neptunFinalEventTitleData is {}
                ? PersonalCalendarEventSpecialType.Final
                : null,
            Location = icalendarEvent.Location,
            Subject = neptunLessonEventTitleData?.Subject,
            Course = neptunLessonEventTitleData?.Course,
            Teachers = neptunLessonEventTitleData?.Teachers,
        };
    }

    // Adatvezérelt szoftverfejlesztés labor ( - L5) - Lucz Géza;Albert István;Tóth Tibor - Tanóra
    // Ergonómia ( - EHU02BM) - Dr. Hercegfi Károly;Dr. Pulay Márk Ágoston - Tanóra
    // Információs rendszerek üzemeltetése ( - L1) - Németh Gábor;Bartalis István Mátyás;Dr. Adamis Gusztáv - Tanóra
    // Mikro- és makroökonómia B ( - EHU20VI) - Haragh Ágnes - Tanóra
    // Flutter alapú szoftverfejlesztés ( - EA) - Pásztor Dániel - Tanóra
    private static void TryParseNeptunLessonTitle(ReadOnlySpan<char> title, out NeptunLessonEventTitleData? result)
    {
        const string subjectCourseSeparator = " ( - ";
        const string courseTeachersSeparator = ") - ";
        const string teachersKindSeparator = " - ";
        const char teacherSeparator = ';';
        const string lessonString = "Tanóra";

        result = null;

        int subjectCourseSeparatorStart = title.IndexOf(subjectCourseSeparator);
        if (subjectCourseSeparatorStart == -1) return;
        ReadOnlySpan<char> subject = title[..subjectCourseSeparatorStart];
        title = title[(subjectCourseSeparatorStart + subjectCourseSeparator.Length)..];
        int courseTeachersSeparatorStart = title.IndexOf(courseTeachersSeparator);
        if (courseTeachersSeparatorStart == -1) return;
        ReadOnlySpan<char> course = title[..courseTeachersSeparatorStart];
        title = title[(courseTeachersSeparatorStart + courseTeachersSeparator.Length)..];
        int teachersKindSeparatorStart = title.IndexOf(teachersKindSeparator);
        if (teachersKindSeparatorStart == -1) return;
        ReadOnlySpan<char> teachersSpan = title[..teachersKindSeparatorStart];
        var teachersEnumerator = teachersSpan.Split(teacherSeparator);
        ReadOnlySpan<char> kindSpan = title[(teachersKindSeparatorStart + teachersKindSeparator.Length)..];
        if (kindSpan is not lessonString) return;
        List<string> teachers = [];
        while (teachersEnumerator.MoveNext())
            teachers.Add(teachersEnumerator.Source[teachersEnumerator.Current].ToString());
        result = new(subject.ToString(), course.ToString(), teachers);
    }
    
    private record struct NeptunLessonEventTitleData(
        string Subject,
        string Course,
        List<string> Teachers
    );
    
    // Automatizált szoftverfejlesztés (Írásbeli) - Dr. Semeráth Oszkár, Dr. Marussy Kristóf - Vizsga
    // Mesterséges intelligencia (Írásbeli) - Dr. Hullám Gábor István - Vizsga
    // Kliensoldali rendszerek (Írásbeli) - Rajacsics Tamás, Albert István, Dr. Kővári Bence András - Vizsga
    // Adatvezérelt rendszerek (Írásbeli) - Benedek Zoltán, Albert István, Imre Gábor, Tóth Tibor - Vizsga
    // Szoftvertechnikák (Írásbeli) - Benedek Zoltán, Albert István - Vizsga
    // Kódolástechnika (Írásbeli) - Dr. Levendovszky János - Vizsga
    // Kommunikációs hálózatok (Írásbeli) - Dr. Simon Vilmos, Dr. Németh Krisztián - Vizsga
    // Számítógépes grafika (Írásbeli) - Dr. Szirmay-Kalos László - Vizsga
    private static void TryParseNeptunFinalTitle(ReadOnlySpan<char> title, out NeptunFinalEventTitleData? result)
    {
        const string subjectAndTypeSeparator = " (";
        const string kindAndTeachersSeparator = ") - ";
        const char teacherSeparator = ',';
        const string end = " - Vizsga";
        
        result = null;

        title = title.TryRemoveFromEnd(end, out bool endCorrect);
        if (!endCorrect) return;

        var kindAndTeachersSeparatorStart = title.IndexOf(kindAndTeachersSeparator);
        if (kindAndTeachersSeparatorStart == -1) return;
        var teachersSpan = title[(kindAndTeachersSeparatorStart + kindAndTeachersSeparator.Length)..];
        var subjectAndKindSpan = title[..kindAndTeachersSeparatorStart];
        
        var subjectAndTypeSeparatorStart = subjectAndKindSpan.LastIndexOf(subjectAndTypeSeparator);
        if (subjectAndTypeSeparatorStart == -1) return;
        var subjectSpan = subjectAndKindSpan[..subjectAndTypeSeparatorStart];
        var kind = subjectAndKindSpan[(subjectAndTypeSeparatorStart + subjectAndTypeSeparator.Length)..];
        
        var teachersEnumerator = teachersSpan.Split(teacherSeparator);
        List<string> teachers = [];
        while (teachersEnumerator.MoveNext())
            teachers.Add(teachersEnumerator.Source[teachersEnumerator.Current].ToString());
        result = new(subjectSpan.ToString(), kind.ToString(), teachers);
    }
    
    private record struct NeptunFinalEventTitleData(
        string Subject,
        string Kind,
        List<string> Teachers
    );
}

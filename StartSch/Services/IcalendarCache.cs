using System.Diagnostics.CodeAnalysis;
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
    public async Task<List<PersonalCalendarEvent>> GetEvents(string url)
    {
        return await memoryCache.GetOrCreateAsync(
            "ical " + url,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                string s = await httpClient.GetStringAsync(url);
                var cal = Calendar.Load(s);
                var res = cal.Events
                    .Select(GetPersonalCalendarEvent)
                    .ToList();
                return res;
            });
    }

    private static PersonalCalendarEvent? GetPersonalCalendarEvent(IcalendarEvent icalendarEvent)
    {
        if (icalendarEvent.Uid is not { } id) return null;
        if (icalendarEvent.Start is not { AsUtc: var startDateTime }) return null;
        if (icalendarEvent.End is not { AsUtc: var endDateTime }) return null;
        if (icalendarEvent.Summary is not { } summary) return null;
        TryParseNeptunLessonTitle(icalendarEvent.Summary!, out var neptunLessonEventTitleData);
        return new()
        {
            Id = id,
            Start = startDateTime.ToInstant(),
            End = endDateTime.ToInstant(),
            Title = neptunLessonEventTitleData != null
                    ? $"{neptunLessonEventTitleData.Value.Subject} {neptunLessonEventTitleData.Value.Course}"
                    : summary,
            Location = icalendarEvent.Location,
            Subject = neptunLessonEventTitleData?.Subject,
            Course = neptunLessonEventTitleData?.Course,
            Teachers = neptunLessonEventTitleData?.Teachers,
        };
    }

    private static bool TryParseNeptunLessonTitle(ReadOnlySpan<char> title,
        [NotNullWhen(true)] out NeptunLessonEventTitleData? result)
    {
        const string subjectCourseSeparator = " ( - ";
        const string courseTeachersSeparator = ") - ";
        const string teachersKindSeparator = " - ";
        const char teacherSeparator = ';';
        const string lessonString = "Tanóra";

        result = null;

        int subjectCourseSeparatorStart = title.IndexOf(subjectCourseSeparator);
        if (subjectCourseSeparatorStart == -1) return false;
        ReadOnlySpan<char> subject = title[..subjectCourseSeparatorStart];
        title = title[(subjectCourseSeparatorStart + subjectCourseSeparator.Length)..];
        int courseTeachersSeparatorStart = title.IndexOf(courseTeachersSeparator);
        if (courseTeachersSeparatorStart == -1) return false;
        ReadOnlySpan<char> course = title[..courseTeachersSeparatorStart];
        title = title[(courseTeachersSeparatorStart + courseTeachersSeparator.Length)..];
        int teachersKindSeparatorStart = title.IndexOf(teachersKindSeparator);
        if (teachersKindSeparatorStart == -1) return false;
        ReadOnlySpan<char> teachersSpan = title[..teachersKindSeparatorStart];
        var teachersEnumerator = teachersSpan.Split(teacherSeparator);
        ReadOnlySpan<char> kindSpan = title[(teachersKindSeparatorStart + teachersKindSeparator.Length)..];
        if (kindSpan is not lessonString) return false;
        List<string> teachers = [];
        while (teachersEnumerator.MoveNext())
            teachers.Add(teachersEnumerator.Source[teachersEnumerator.Current].ToString());
        result = new(subject.ToString(), course.ToString(), teachers);
        return true;
    }

    private record struct NeptunLessonEventTitleData(
        string Subject,
        string Course,
        List<string> Teachers
    );
}

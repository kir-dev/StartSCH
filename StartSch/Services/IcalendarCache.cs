using Ical.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using StartSch.Data;
using StartSch.Modules.PortalVikBmeHu;
using StartSch.Wasm.PersonalCalendars;
using IcalendarEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace StartSch.Services;

// TODO: Rename to IcalendarService
public class IcalendarCache(
    IMemoryCache memoryCache,
    HttpClient httpClient,
    ILogger<IcalendarCache> logger,
    IDbContextFactory<Db> dbFactory,
    PortalVikBmeHuModule? portalVikBmeHuModule = null
)
{
    private static readonly TimeSpan MemoryCacheDuration = TimeSpan.FromHours(1);
    private static readonly Duration MaxCacheAge = Duration.FromHours(48);

    // TODO: return error events when URL is invalid or return an error to be handled that can then be turned into events
    public async Task<List<PersonalCalendarEvent>> GetEvents(string url, Type externalCalendarType)
    {
        string cacheKey = $"ical {externalCalendarType.Name} {url}";
        if (memoryCache.TryGetValue(cacheKey, out List<PersonalCalendarEvent>? cached) && cached is not null)
            return cached;

        string rawIcs;
        try
        {
            rawIcs = await httpClient.GetStringAsync(url);
        }
        catch (HttpRequestException exception)
        {
            logger.LogInformation(exception, ".ics request failed, serving cached response if available");
            return await GetCachedOrEmpty(url, externalCalendarType);
        }

        // A successfully loaded calendar means a genuine upstream success: even an empty calendar is
        // authoritative and must overwrite any cached copy. A null parse is treated like a failed
        // request and falls back to the cache so events are not dropped during an upstream hiccup.
        Calendar? calendar = Calendar.Load(rawIcs);
        if (calendar == null)
        {
            logger.LogWarning("Unparseable .ics for {Url}; serving cached response if available", url);
            return await GetCachedOrEmpty(url, externalCalendarType);
        }

        var events = calendar.Events
            .Select(icalendarEvent => GetPersonalCalendarEvent(icalendarEvent, externalCalendarType)!)
            .ToList();

        memoryCache.Set(cacheKey, events, MemoryCacheDuration);
        await TryPersistToDbAsync(url, rawIcs);
        return events;
    }

    private async Task<List<PersonalCalendarEvent>> GetCachedOrEmpty(string url, Type externalCalendarType)
    {
        byte[] urlHash = CachedIcsCrypto.DeriveLookupHash(url);

        CachedIcsResponse? row;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            row = await db.CachedIcsResponses
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.UrlHash == urlHash);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read cached .ics for {Url}", url);
            return [];
        }

        if (row is null || SystemClock.Instance.GetCurrentInstant() - row.UpdatedAt > MaxCacheAge)
            return [];

        try
        {
            string rawIcs = CachedIcsCrypto.Decrypt(url, row.Data, row.Nonce, row.Tag, row.Version);
            Calendar? calendar = Calendar.Load(rawIcs);
            if (calendar == null)
                return [];
            return calendar.Events
                .Select(icalendarEvent => GetPersonalCalendarEvent(icalendarEvent, externalCalendarType)!)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to decrypt cached .ics for {Url}", url);
            return [];
        }
    }

    private async Task TryPersistToDbAsync(string url, string rawIcs)
    {
        try
        {
            (byte[] data, byte[] nonce, byte[] tag) = CachedIcsCrypto.Encrypt(url, rawIcs);
            byte[] urlHash = CachedIcsCrypto.DeriveLookupHash(url);
            var updatedAt = SystemClock.Instance.GetCurrentInstant();

            await using var db = await dbFactory.CreateDbContextAsync();
            var row = await db.CachedIcsResponses.FirstOrDefaultAsync(r => r.UrlHash == urlHash);
            if (row is null)
            {
                db.CachedIcsResponses.Add(new CachedIcsResponse
                {
                    UrlHash = urlHash,
                    UpdatedAt = updatedAt,
                    Data = data,
                    Nonce = nonce,
                    Tag = tag,
                    Version = CachedIcsCrypto.CurrentSchemeVersion,
                });
            }
            else
            {
                row.UpdatedAt = updatedAt;
                row.Data = data;
                row.Nonce = nonce;
                row.Tag = tag;
                row.Version = CachedIcsCrypto.CurrentSchemeVersion;
            }

            await db.SaveChangesAsync();
        }
        catch (Exception exception)
        {
            // The cache is best-effort: never let a failed cache write break serving live events.
            logger.LogWarning(exception, "Failed to persist cached .ics");
        }
    }

    private PersonalCalendarEvent? GetPersonalCalendarEvent(IcalendarEvent icalendarEvent,
        Type externalCalendarType)
    {
        if (icalendarEvent is not
            {
                Uid: { } id,
                Start.AsUtc: var startDateTime,
                End.AsUtc: var endDateTime,
                Summary: { } summary,
            })
            return null;

        NeptunLessonEventTitleData? neptunLessonEventTitleData = null;
        NeptunFinalEventTitleData? neptunFinalEventTitleData = null;
        NeptunTaskEventTitleData? neptunTaskEventTitleData = null;
        SubjectData? subjectData = null;
        string? subjectId = null;

        if (externalCalendarType == typeof(PersonalNeptunCalendarLive))
        {
            TryParseNeptunLessonTitle(icalendarEvent.Summary!, out neptunLessonEventTitleData);
            TryParseNeptunFinalTitle(icalendarEvent.Summary!, out neptunFinalEventTitleData);
            TryParseNeptunTaskTitle(icalendarEvent.Summary!, out neptunTaskEventTitleData);
        }

        if (externalCalendarType == typeof(PersonalMoodleCalendarLive))
        {
            if (icalendarEvent.Categories is [{ } category])
            {
                subjectId = category;
                subjectData = portalVikBmeHuModule?.GetSubject(subjectId);
            }
        }

        return new()
        {
            Id = id,
            Start = startDateTime.ToInstant(),
            End = endDateTime.ToInstant(),
            Title =
                neptunLessonEventTitleData is { }
                    ? $"{neptunLessonEventTitleData.Value.Subject} {neptunLessonEventTitleData.Value.Course}"
                    : neptunFinalEventTitleData is { }
                        ? $"{neptunFinalEventTitleData.Value.Subject} vizsga ({neptunFinalEventTitleData.Value.Kind})"
                        : neptunTaskEventTitleData is { }
                            ? neptunTaskEventTitleData.Value.Title
                            : summary,
            SpecialType = neptunFinalEventTitleData is { }
                ? PersonalCalendarEventSpecialType.Final
                : null,
            Location = icalendarEvent.Location,
            SubjectId = subjectId,
            Subject = neptunLessonEventTitleData?.Subject
                      ?? subjectData?.Name,
            Course = neptunLessonEventTitleData?.Course,
            Teachers = neptunLessonEventTitleData?.Teachers
                       ?? neptunFinalEventTitleData?.Teachers,
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
    // Szoftvertechnikák (Írásbeli) - Dr. Levendovszky János - Vizsga
    // Kódolástechnika (Írásbeli) - Dr. Simon Vilmos, Dr. Németh Krisztián - Vizsga
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

    // pótzh (összegző értékelés) - Feladat - Határidő: 17:00:00
    // Késés miatt kiírandó különeljárási díjak száma (majd kiírandó tanszéki különeljárási díj) - Feladat - Határidő: 0:00:00
    private static void TryParseNeptunTaskTitle(ReadOnlySpan<char> title, out NeptunTaskEventTitleData? result)
    {
        const string endWithoutTime = " - Feladat - Határidő: ";
        const char timeSeparator = ':';
        const int longTimeLength = 2 + 1 + 2 + 1 + 2;
        int minLength = 2 + endWithoutTime.Length + longTimeLength;
        result = null;
        if (title.Length < minLength) return;
        if (title[^3] != timeSeparator) return;
        if (title[^6] != timeSeparator) return;
        int timeLength = title[^8] == ' ' ? longTimeLength - 1 : longTimeLength;
        title = title[..^timeLength];
        if (!title.TryRemoveFromEnd(endWithoutTime)) return;
        result = new(title.ToString());
    }

    private record struct NeptunTaskEventTitleData(
        string Title
    );
}

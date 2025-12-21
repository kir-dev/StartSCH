using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime.Serialization.SystemTextJson;
using StartSch.Data;
using StartSch.Data.Migrations;

namespace StartSch;

public static class Utils
{
    public static CultureInfo HungarianCulture { get; } = new("hu-HU");
    public static DateTimeZone HungarianTimeZone { get; } = DateTimeZoneProviders.Tzdb["Europe/Budapest"];

    static Utils()
    {
        // https://e-nyelv.hu/2007-08-28/a-het-napjai/
        // https://e-nyelv.hu/2017-01-03/napok-roviditese/
        HungarianCulture.DateTimeFormat.AbbreviatedDayNames =
        [
            "v.",
            "h.",
            "k.",
            "sze.",
            "cs.",
            "p.",
            "szo.",
        ];
    }

    // Used to match Pek names to Pincer names
    public static bool RoughlyMatches(this string a, string b)
    {
        a = a.Simplify();
        b = b.Simplify();
        return a.Contains(b) || b.Contains(a);
    }

    // Lowercase, remove diacritics, remove symbols
    public static string Simplify(this string str)
    {
        // TODO: use spans to avoid allocation
        str = str.ToLower(HungarianCulture);
        StringBuilder sb = new(str.Length);
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i] switch
            {
                'á' => 'a',
                'é' => 'e',
                'í' => 'i',
                'ó' => 'o',
                'ö' => 'o',
                'ő' => 'o',
                'ú' => 'u',
                'ü' => 'u',
                'ű' => 'u',
                _ => str[i]
            };
            if (c is (>= 'a' and <= 'z') or (>= '0' and <= '9'))
                sb.Append(c);
        }

        return sb.ToString();
    }

    /// Returns s with at most length characters
    public static string Trim(this string s, int length)
    {
        if (s.Length <= length)
            return s;
        return s[..length];
    }

    /// Registers a type as a singleton and a hosted service
    public static void AddSingletonAndHostedService<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
            TService>
        (this IServiceCollection serviceCollection)
        where TService : class, IHostedService
    {
        serviceCollection.AddSingleton<TService>();
        serviceCollection.AddHostedService<TService>(sp => sp.GetRequiredService<TService>());
    }

    public static string? GetVerifiedEmailAddress(this User user)
    {
        return user is { StartSchEmail: { } addr, StartSchEmailVerified: true }
            ? addr
            : user.AuthSchEmail;
    }

    public static Task<IDbContextTransaction> BeginTransaction(
        this Db db,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default
    )
    {
        // SQLite is implicitly isolated and doesn't seem to support setting the isolation level
        return db is SqliteDb
            ? db.Database.BeginTransactionAsync(cancellationToken)
            : db.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public static int GetId(this ClaimsPrincipal claimsPrincipal)
    {
        string? value = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
        if (value != null)
            return int.Parse(value);
        throw new InvalidOperationException("User ID not found in claims");
    }

    public static Guid? GetAuthSchId(this ClaimsPrincipal claimsPrincipal)
    {
        string? value = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (value != null)
            return Guid.Parse(value);
        return null;
    }

    public static List<Page> GetOwners(this Event @event) => @event.Categories.GetOwners();

    public static List<Page> GetOwners(this Post post) => post.Categories.GetOwners();

    public static List<Page> GetOwners(this List<Category> categories) =>
        categories
            .Select(c => c.Page)
            .Distinct()
            .ToList();

    public static string GetName(this Page page) => page.Name ?? page.PincerName ?? page.PekName ?? "Névtelen oldal";

    public static async Task<TResult> HandleHttpExceptions<TResult>(
        this Task<TResult> task,
        bool notFoundMeansUnavailable = false)
    {
        // exceptions are such a beautiful solution to error handling /s
        try
        {
            return await task;
        }
        catch (HttpRequestException httpRequestException)
            when (
                httpRequestException.HttpRequestError
                    is HttpRequestError.NameResolutionError
                    or HttpRequestError.SecureConnectionError
                || (httpRequestException.StatusCode is HttpStatusCode.NotFound && notFoundMeansUnavailable)
                || httpRequestException.StatusCode
                    is HttpStatusCode.ServiceUnavailable // nginx when backing k8s deployment scaled to 0
            )
        {
            throw new ModuleUnavailableException(httpRequestException);
        }
        catch (TaskCanceledException taskCanceledException)
            when (taskCanceledException.InnerException
                      is TimeoutException
                      or HttpRequestException { HttpRequestError: HttpRequestError.SecureConnectionError }
                 )
        {
            throw new ModuleUnavailableException(taskCanceledException);
        }
    }

    /// For dates stored using SQLite, EF doesn't persist the Kind. Set it to UTC.
    [Pure]
    public static DateTime FixDateTimeKind(this DateTime dateTime)
    {
        // short-circuit for Postgres
        if (dateTime.Kind == DateTimeKind.Utc)
            return dateTime;

        return dateTime.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => throw new InvalidOperationException(),
            _ => throw new ArgumentOutOfRangeException(nameof(dateTime))
        };
    }

    [Pure]
    public static string? IfNotEmpty(this string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s;


    // stolen from https://github.com/Humanizr/Humanizer/blob/c047a97de908acdc39b7aec3ea772e88173327f0/src/Humanizer/FluentDate/PrepositionsExtensions.cs#L13
    public static DateTime At(this DateTime date, int hour, int min = 0, int second = 0, int millisecond = 0)
    {
        return new(date.Year, date.Month, date.Day, hour, min, second, millisecond);
    }

    public static ReadOnlySpan<char> RemoveFromStart(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
    {
        return span.StartsWith(value)
            ? span[value.Length..]
            : throw new ArgumentException("Span does not start with value", nameof(span));
    }

    public static ReadOnlySpan<char> TryRemoveFromStart(this ReadOnlySpan<char> span, ReadOnlySpan<char> value)
    {
        return span.StartsWith(value)
            ? span[value.Length..]
            : span;
    }

    public static ReadOnlySpan<char> RemoveFromEnd(this ReadOnlySpan<char> span, char value)
    {
        return span[^1] == value
            ? span[..^1]
            : throw new ArgumentException("Span does not end with value", nameof(span));
    }

    /// Assumes weeks start on Monday
    public static LocalDate GetMondayOfWeekOf(LocalDate date)
    {
        var dayOfWeek = date.DayOfWeek;
        int offset = (int)dayOfWeek - 1;
        return date.PlusDays(offset);
    }

    /// Assumes weeks start on Monday
    public static LocalDate GetSundayOfWeekOf(LocalDate date)
    {
        var dayOfWeek = date.DayOfWeek;
        return date.PlusDays(-(int)dayOfWeek + 1);
    }

    // https://www.rfc-editor.org/rfc/rfc5545.html#section-3.6.1
    public static (LocalDate Start, LocalDate End) AllDayGetDates(Instant start, Instant? end)
    {
        ZonedDateTime startHu = start.InZone(HungarianTimeZone);
        LocalDate startDate = startHu.Date.PlusDays(startHu.Hour > 12 ? 1 : 0);
        if (end == null)
            return (startDate, startDate.PlusDays(1));

        ZonedDateTime endHu = end.Value.InZone(HungarianTimeZone);
        LocalDate endDate = endHu.Date.PlusDays(endHu.Hour > 12 ? 1 : 0);
        if (endDate <= startDate)
            return (startDate, startDate.PlusDays(1));

        return (startDate, endDate);
    }

    /// Postgres compatible TimeOnly.MaxValue.
    ///
    /// Postgres rounds TimeOnly.MaxValue to the start of the next day, as it doesn't store nanoseconds.
    public static TimeOnly EndOfDay { get; } = new(23, 59, 59, 999);

    /// Lower-case key names with support for Noda Time
    public static JsonSerializerOptions JsonSerializerOptions { get; } =
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
}

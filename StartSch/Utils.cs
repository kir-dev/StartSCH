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
using StartSch.Data;
using StartSch.Data.Migrations;

namespace StartSch;

public static class Utils
{
    // TODO: remove when updating to .NET 9
    public static JsonSerializerOptions JsonSerializerOptionsWeb { get; } = new(JsonSerializerDefaults.Web);

    public static CultureInfo HungarianCulture { get; } = new("hu-HU");
    public static TimeZoneInfo HungarianTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");

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

    public static string FormatDateRange(DateTime startUtc, DateTime? endUtc)
    {
        DateTime start = TimeZoneInfo.ConvertTimeFromUtc(startUtc, HungarianTimeZone);
        string result = start.ToString("f", HungarianCulture);
        if (!endUtc.HasValue)
            return result;

        DateTime end = TimeZoneInfo.ConvertTimeFromUtc(endUtc.Value, HungarianTimeZone);
        return result + "-" + end.ToString("t", HungarianCulture);
    }

    public static string FormatDateFull(DateTime dateUtc)
    {
        DateTime date = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, HungarianTimeZone);
        StringBuilder sb = new(96);
        sb.Append(date.ToString("D", HungarianCulture));
        sb.Append(' ');
        sb.Append(date.ToString("t", HungarianCulture));
        sb.Append(' ');
        sb.Append('(');
        var zoneId = HungarianTimeZone.IsDaylightSavingTime(dateUtc)
            ? HungarianTimeZone.DaylightName
            : HungarianTimeZone.StandardName;
        sb.Append(zoneId);
        sb.Append(')');
        return sb.ToString();
    }

    public static string FormatDate(DateTime dateUtc)
    {
        DateTime nowUtc = DateTime.UtcNow;
        DateTime date = TimeZoneInfo.ConvertTimeFromUtc(dateUtc, HungarianTimeZone);
        DateTime now = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, HungarianTimeZone);

        return dateUtc > nowUtc
            ? FormatFutureDate(dateUtc - nowUtc, date, now)
            : FormatPastDate(nowUtc - dateUtc, date, now);
    }

    private static string FormatPastDate(TimeSpan timeSince, DateTime date, DateTime now)
    {
        if (timeSince.TotalMinutes < 59)
            return $"{timeSince.TotalMinutes:0} perce";
        if (timeSince.TotalHours < 10)
            return $"{timeSince.TotalHours:0} órája";
        if (date.Date == now.Date)
            return "ma, " + date.ToString("t", HungarianCulture);
        if (date.Date == now.Date.AddDays(-1))
            return "tegnap";
        if (date.Date == now.Date.AddDays(-2))
            return "tegnapelőtt";
        if (timeSince.TotalDays < 7)
            return $"{timeSince.TotalDays:0} napja";
        if (date.Year == now.Year)
            return date.ToString("MMM dd.", HungarianCulture);
        return date.ToString("yy. MMM dd.", HungarianCulture);
    }

    private static string FormatFutureDate(TimeSpan timeUntil, DateTime date, DateTime now)
    {
        if (timeUntil.TotalMinutes < 59)
            return $"{timeUntil.TotalMinutes:0} perc múlva";
        if (timeUntil.TotalHours < 10)
            return $"{timeUntil.TotalHours:0} óra múlva";
        if (date.Date == now.Date)
            return "ma, " + date.ToString("t", HungarianCulture);
        if (date.Date == now.Date.AddDays(1))
            return "holnap, " + date.ToString("t", HungarianCulture);
        if (date.Date == now.Date.AddDays(-2))
            return "holnapután, " + date.ToString("t", HungarianCulture);
        if (timeUntil.TotalDays < 7)
            return date.ToString("dddd, HH:mm", HungarianCulture);
        if (date.Year == now.Year)
            return date.ToString("MMM dd.", HungarianCulture);
        return date.ToString("yy. MMM dd.", HungarianCulture);
    }

    public static DateTime HungarianToUtc(this DateTime dateTime)
    {
        return TimeZoneInfo.ConvertTimeToUtc(dateTime, HungarianTimeZone);
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
        // exceptions are such a beautiful solution to error handling
        try
        {
            return await task;
        }
        catch (HttpRequestException httpRequestException)
            when (httpRequestException.HttpRequestError
                      is HttpRequestError.NameResolutionError
                      or HttpRequestError.SecureConnectionError
                  || (httpRequestException.StatusCode is HttpStatusCode.NotFound && notFoundMeansUnavailable))
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
}

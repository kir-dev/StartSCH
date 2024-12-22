using System.Globalization;
using System.Text;
using System.Text.Json;
using Markdig;

namespace StartSch;

public static class Utils
{
    // TODO: remove when updating to .NET 9
    public static JsonSerializerOptions JsonSerializerOptionsWeb { get; } = new(JsonSerializerDefaults.Web);

    public static MarkdownPipeline MarkdownPipeline { get; } = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .Build();

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

    // Trim and collapse multiple new lines or spaces into one.
    // A whitespace group with any newlines is replaced with a single newline.
    private static readonly Rune Newline = new('\n');
    private static readonly Rune Space = new(' ');

    public static string ToAndroidNotificationBody(this ReadOnlySpan<char> s)
    {
        StringBuilder sb = new(s.Length);
        SpanRuneEnumerator runes = s.Trim().EnumerateRunes();
        Rune? whitespace = null;
        while (runes.MoveNext())
        {
            var curr = runes.Current;
            if (Rune.IsWhiteSpace(runes.Current))
            {
                if (whitespace == Newline)
                    continue;
                if (curr == Newline || curr == Space)
                    whitespace = curr;
            }
            else
            {
                if (whitespace.HasValue)
                {
                    sb.Append(whitespace.Value);
                    whitespace = null;
                }

                sb.Append(runes.Current);
            }
        }

        return sb.ToString();
    }
}
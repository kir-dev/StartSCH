using System.Text;

namespace StartSch;

public static class ExcerptExtensions
{
    private static readonly Rune Newline = new('\n');
    private static readonly Rune Space = new(' ');

    /// Trim leading and trailing whitespace, collapse contiguous whitespace characters and limit length.
    ///
    /// Contiguous whitespace characters with any newlines are replaced with a single newline, otherwise a space.
    /// Mimics how Android displays notification body text.
    public static string ToExcerpt(this ReadOnlySpan<char> s)
    {
        StringBuilder sb = new(s.Length);
        SpanRuneEnumerator runes = s.Trim().EnumerateRunes();
        Rune? whitespace = null;
        uint count = 0; // count UTF8 chars instead of using StringBuilder.Length as it uses UTF16
        while (runes.MoveNext() && count < 399)
        {
            Rune curr = runes.Current;
            if (Rune.IsWhiteSpace(runes.Current))
            {
                if (whitespace == Newline)
                    continue;

                if (curr == Newline)
                    whitespace = Newline;
                else
                    whitespace = Space;
            }
            else
            {
                if (whitespace.HasValue)
                {
                    sb.Append(whitespace.Value);
                    count++;
                    whitespace = null;
                }

                sb.Append(runes.Current);
                count++;
            }
        }

        return sb.ToString();
    }
}
namespace StartSch.Wasm;

public static class SharedStringExtensions
{
    extension(ReadOnlySpan<char> span)
    {
        public ReadOnlySpan<char> RemoveFromStart(ReadOnlySpan<char> value)
        {
            return span.StartsWith(value)
                ? span[value.Length..]
                : throw new ArgumentException("Span does not start with value", nameof(span));
        }

        public ReadOnlySpan<char> TryRemoveFromStart(ReadOnlySpan<char> value)
        {
            return span.StartsWith(value)
                ? span[value.Length..]
                : span;
        }

        public ReadOnlySpan<char> RemoveFromEnd(char value)
        {
            return span[^1] == value
                ? span[..^1]
                : throw new ArgumentException("Span does not end with value", nameof(span));
        }

        public ReadOnlySpan<char> RemoveFromEnd(string value)
        {
            return span.EndsWith(value)
                ? span[..^value.Length]
                : throw new ArgumentException("Span does not end with value", nameof(span));
        }

        public ReadOnlySpan<char> TryRemoveFromEnd(string value, out bool successful)
        {
            if (span.EndsWith(value))
            {
                successful = true;
                return span[..^value.Length];
            }

            successful = false;
            return span;
        }
    }

    extension(ref ReadOnlySpan<char> span)
    {
        public bool TryRemoveFromEnd(string value)
        {
            if (!span.EndsWith(value))
                return false;
            span = span[..^value.Length];
            return true;

        }
    }
}

namespace StartSch;

public record Post(
    string Title,
    string Excerpt,
    string Body,
    string Url,
    DateTime PublishedAtUtc,
    IEnumerable<string> Tags);
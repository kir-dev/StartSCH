namespace StartSch;

public record Event(
    string Title,
    string Excerpt,
    string Body,
    DateTime StartUtc,
    DateTime EndUtc,
    string Url,
    IEnumerable<string> Tags);
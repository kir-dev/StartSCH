namespace StartSch;

public record PushNotification(
    string Title,
    string Body,
    string? Icon,
    string? Url
);
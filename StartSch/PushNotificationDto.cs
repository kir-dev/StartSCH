using JetBrains.Annotations;

namespace StartSch;

[UsedImplicitly]
public record PushNotificationDto(
    string Title,
    string Body,
    string? Url,
    string? Icon
);
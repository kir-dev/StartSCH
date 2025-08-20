using JetBrains.Annotations;

namespace StartSch;

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
public record PushNotificationDto(
    string Title,
    string? Body,
    string Url,
    string? Icon
);

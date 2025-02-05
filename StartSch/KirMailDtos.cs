using JetBrains.Annotations;

namespace StartSch;

// https://mail.kir-dev.hu/api

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
public record SingleSendRequestDto(
    FromDto From,
    string To,
    string Subject,
    string Html,
    string? ReplyTo = null,
    string? Queue = null
);

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
public record MultipleSendRequestDto(
    FromDto From,
    List<string> To,
    string Subject,
    string Html,
    string? ReplyTo = null,
    string? Queue = null
);

[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
public record FromDto(
    string Name,
    string? Email // everything just gets sent from noreply@m1.kir-dev.hu
);

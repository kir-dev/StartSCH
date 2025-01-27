namespace StartSch;

public record EmailVerificationData(
    Guid UserId,
    string Email
);
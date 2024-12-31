using Microsoft.AspNetCore.Authorization;

namespace StartSch.Auth.Requirements;

public record ResourceAccessRequirement(AccessLevel AccessLevel) : IAuthorizationRequirement
{
    public static ResourceAccessRequirement Read { get; } = new(AccessLevel.Read);
    public static ResourceAccessRequirement Write { get; } = new(AccessLevel.Write);
}
using Microsoft.AspNetCore.Authorization;

namespace StartSch.Auth.Requirements;

public record ResourceAccessRequirement(AccessLevel AccessLevel) : IAuthorizationRequirement
{
    public static ResourceAccessRequirement Read { get; } = new(AccessLevel.Read);

    /// The user can create, delete, modify, or save the given resource to the
    /// database in its current state.
    ///
    /// For example, when replacing a Post's Event, the user must have access to
    /// the post both before, and after modifying it.
    public static ResourceAccessRequirement Write { get; } = new(AccessLevel.Write);
}
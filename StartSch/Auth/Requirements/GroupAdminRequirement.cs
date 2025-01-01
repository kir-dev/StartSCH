using Microsoft.AspNetCore.Authorization;

namespace StartSch.Auth.Requirements;

/// Requires being an admin of the given Group resource
public class GroupAdminRequirement : IAuthorizationRequirement
{
    public static GroupAdminRequirement Instance { get; } = new();
}
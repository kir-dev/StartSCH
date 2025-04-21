using Microsoft.AspNetCore.Authorization;

namespace StartSch.Authorization.Requirements;

/// Requires being an admin of the given Group resource
public class PageAdminRequirement : IAuthorizationRequirement
{
    public static PageAdminRequirement Instance { get; } = new();
}
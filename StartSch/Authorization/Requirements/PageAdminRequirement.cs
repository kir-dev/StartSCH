using Microsoft.AspNetCore.Authorization;

namespace StartSch.Authorization.Requirements;

/// Requires being an admin of the given Page resource
public class PageAdminRequirement : IAuthorizationRequirement
{
    public const string Policy = "PageAdmin";
    public static PageAdminRequirement Instance { get; } = new();
}

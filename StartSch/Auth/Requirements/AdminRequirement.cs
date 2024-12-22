using Microsoft.AspNetCore.Authorization;

namespace StartSch.Auth.Requirements;

/// Signifies that the user has admin access to at least one of the modules, and can therefore access the admin panel.
public class AdminRequirement : IAuthorizationRequirement
{
}
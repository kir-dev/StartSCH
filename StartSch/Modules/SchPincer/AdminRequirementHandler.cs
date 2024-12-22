using Microsoft.AspNetCore.Authorization;
using StartSch.Auth.Requirements;

namespace StartSch.Modules.SchPincer;

public class AdminRequirementHandler(SchPincerModule pincerModule) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRequirement requirement
    )
    {
        var adminMemberships = context.User.GetAdminMemberships();
        var pincerGroups = await pincerModule.GetGroups();
        var match = pincerGroups
            .Where(g => g.PekId.HasValue)
            .Select(g => g.PekId!.Value)
            .Intersect(adminMemberships.Select(m => m.PekId))
            .Any();
        if (match)
            context.Succeed(requirement);
    }
}
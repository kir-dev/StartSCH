using Microsoft.AspNetCore.Authorization;
using StartSch.Authorization.Requirements;
using StartSch.Data;

namespace StartSch.Modules.SchPincer;

public class GroupAdminHandler(SchPincerModule pincerModule) : AuthorizationHandler<GroupAdminRequirement, Group>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GroupAdminRequirement requirement,
        Group group)
    {
        var adminMemberships = context.User.GetAdminMemberships();
        if (adminMemberships.All(m => m.PekId != group.PekId))
            return;
        var pincerGroups = await pincerModule.GetGroups();
        if (pincerGroups.All(g => g.Id != group.Id))
            return;
        context.Succeed(requirement);
    }
}
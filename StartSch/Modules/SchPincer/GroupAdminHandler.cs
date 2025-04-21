using Microsoft.AspNetCore.Authorization;
using StartSch.Authorization.Requirements;
using StartSch.Data;

namespace StartSch.Modules.SchPincer;

public class GroupAdminHandler(SchPincerModule pincerModule) : AuthorizationHandler<PageAdminRequirement, Page>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PageAdminRequirement requirement,
        Page page)
    {
        var adminMemberships = context.User.GetAdminMemberships();
        if (adminMemberships.All(m => m.PekId != page.PekId))
            return;
        var pincerGroups = await pincerModule.GetGroups();
        if (pincerGroups.All(g => g.Id != page.Id))
            return;
        context.Succeed(requirement);
    }
}
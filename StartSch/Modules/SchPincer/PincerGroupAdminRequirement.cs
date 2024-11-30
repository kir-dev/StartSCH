using Microsoft.AspNetCore.Authorization;
using StartSch.Data;

namespace StartSch.Modules.SchPincer;

/// Specifies that the user must be an admin of the group for the requested page.
public class PincerGroupAdminRequirement : AuthorizeAttribute, IAuthorizationRequirement, IAuthorizationRequirementData
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}

public class PincerGroupAdminRequirementHandler(Db db) : AuthorizationHandler<PincerGroupAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PincerGroupAdminRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return;

        if (!httpContext.Request.RouteValues.TryGetValue("GroupId", out object? groupIdObject))
            throw new($"{nameof(PincerGroupAdminRequirement)} requires a GroupId parameter in the route pattern.");

        if (!int.TryParse((string)groupIdObject!, out int groupId))
            throw new("GroupId must be an int.");

        List<GroupMembership>? memberships = context.User.GetGroupMemberships();
        if (memberships == null)
            return;

        Group? group = await db.Groups.FindAsync(groupId);
        if (group == null)
            return;

        if (memberships.Any(m => m.PekId == group.PekId))
            context.Succeed(requirement);
    }
}
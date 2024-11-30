using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Modules.SchPincer;

public class PincerAdminRequirement : AuthorizeAttribute, IAuthorizationRequirement, IAuthorizationRequirementData
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}

public class PincerAdminRequirementHandler(Db db) : AuthorizationHandler<PincerAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PincerAdminRequirement requirement)
    {
        var memberships = context.User.GetGroupMemberships()?
            .Where(g => g.Titles.Any(t => t.RoughlyMatches("korvez") || t.RoughlyMatches("admin") || t.Contains("PR")))
            .Select(g => g.PekId)
            .ToList();
        if (memberships == null)
            return;
        if (await db.Groups.AnyAsync(g => memberships.Contains(g.PekId!.Value)))
            context.Succeed(requirement);
    }
}

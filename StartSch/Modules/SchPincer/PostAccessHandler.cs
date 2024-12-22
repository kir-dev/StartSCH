using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using StartSch.Data;

namespace StartSch.Modules.SchPincer;

/// Allows reading/updating/deleting a post if the user is an admin for at least one of the groups that wrote the post.
public class PostAccessHandler : AuthorizationHandler<OperationAuthorizationRequirement, Post>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Post post
    )
    {
        var adminMemberships = context.User.GetAdminMemberships();
        var authorGroups = post.Groups;
        bool match = authorGroups
            .Where(g => g.PekId.HasValue)
            .Select(g => g.PekId!.Value)
            .Intersect(adminMemberships.Select(m => m.PekId))
            .Any();
        if (match)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
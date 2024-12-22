using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using StartSch.Data;

namespace StartSch.Auth.Requirements;

/// Allows reading a post if it has been published.
public class PublishedPostAccessHandler : AuthorizationHandler<OperationAuthorizationRequirement, Post>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Post post)
    {
        if (requirement.Name != "Read")
            return Task.CompletedTask;

        if (post.PublishedUtc.HasValue)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
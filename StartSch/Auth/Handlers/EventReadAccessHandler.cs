using Microsoft.AspNetCore.Authorization;
using StartSch.Auth.Requirements;
using StartSch.Data;

namespace StartSch.Auth.Handlers;

/// Allows reading any event
public class EventReadAccessHandler : AuthorizationHandler<ResourceAccessRequirement, Event>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ResourceAccessRequirement requirement, Event resource)
    {
        if (requirement.AccessLevel == AccessLevel.Read)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
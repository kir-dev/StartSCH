using Microsoft.AspNetCore.Authorization;
using StartSch.Authorization.Requirements;
using StartSch.Data;

namespace StartSch.Authorization.Handlers;

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
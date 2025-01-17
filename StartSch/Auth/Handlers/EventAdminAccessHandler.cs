using Microsoft.AspNetCore.Authorization;
using StartSch.Auth.Requirements;
using StartSch.Data;

namespace StartSch.Auth.Handlers;

/// Grant write access to an event if the user is an admin at least one of the event's groups
public class EventAdminAccessHandler(IServiceProvider serviceProvider) : AuthorizationHandler<ResourceAccessRequirement, Event>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAccessRequirement requirement,
        Event @event
    )
    {
        if (requirement.AccessLevel != AccessLevel.Write)
            return; // let the other handler handle this

        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        foreach (Group group in @event.Groups)
        {
            var res = await authorizationService.AuthorizeAsync(
                context.User,
                group,
                GroupAdminRequirement.Instance);
            if (res.Succeeded)
                context.Succeed(requirement);
            return;
        }
    }
}
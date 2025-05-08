using Microsoft.AspNetCore.Authorization;
using StartSch.Authorization.Requirements;
using StartSch.Data;

namespace StartSch.Authorization.Handlers;

/// Grant write access to an event if the user is an admin of at least one of the event's groups
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

        foreach (Page page in @event.Categories.Select(c => c.Page))
        {
            var res = await authorizationService.AuthorizeAsync(
                context.User,
                page,
                PageAdminRequirement.Instance);
            if (res.Succeeded)
                context.Succeed(requirement);
            return;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using StartSch.Authorization.Requirements;
using StartSch.Data;

namespace StartSch.Authorization.Handlers;

/// Grant read/write access to a post if:
/// - the post is owned by a single group and the user is an admin of it,
/// - or the user is an admin of one of the post's event's groups.
public class PostAdminAccessHandler(IServiceProvider serviceProvider) : AuthorizationHandler<ResourceAccessRequirement, Post>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResourceAccessRequirement requirement,
        Post post
    )
    {
        var authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();

        List<Page> postOwners = post.GetOwners();
        if (postOwners is [{ } postOwner])
        {
            var res = await authorizationService.AuthorizeAsync(
                context.User,
                postOwner,
                PageAdminRequirement.Instance);
            if (res.Succeeded)
                context.Succeed(requirement);
            return;
        }

        if (post.Event != null)
        {
            foreach (Page eventOwner in post.Event.GetOwners())
            {
                var res = await authorizationService.AuthorizeAsync(
                    context.User,
                    eventOwner,
                    PageAdminRequirement.Instance);
                if (res.Succeeded)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}

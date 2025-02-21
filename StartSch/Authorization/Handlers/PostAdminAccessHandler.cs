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

        if (post.Groups.Count == 1)
        {
            var group = post.Groups[0];
            var res = await authorizationService.AuthorizeAsync(
                context.User,
                group,
                GroupAdminRequirement.Instance);
            if (res.Succeeded)
                context.Succeed(requirement);
            return;
        }

        if (post.Event != null)
        {
            foreach (Group group in post.Event.Groups)
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
}
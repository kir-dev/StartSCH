using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StartSch.Auth.Requirements;
using StartSch.Data;

namespace StartSch.Modules.SchPincer;

public class PostWriteRequirementHandler(Db db) : AuthorizationHandler<PostWriteRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PostWriteRequirement requirement)
    {
        Post? post = await GetPost(context);
        if (post == null) return;

        var adminMemberships = context.User
            .GetAdminMemberships()
            .Select(m => m.PekId);
        var groups = post.Groups
            .Where(g => g.PekId.HasValue)
            .Select(g => g.PekId!.Value);
        if (adminMemberships.Intersect(groups).Any())
            context.Succeed(requirement);
    }

    private async Task<Post?> GetPost(AuthorizationHandlerContext context)
    {
        if (context.Resource is Post post)
            return post;
        if (context.TryGetRouteParameter<int>("PostId") is not { } id)
            return null;
        return await db.Posts
            .Include(p => p.Groups)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}

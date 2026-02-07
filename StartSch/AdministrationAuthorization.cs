using System.Collections.Frozen;
using System.Security.Claims;
using StartSch.Data;

namespace StartSch;

public static class AdministrationAuthorization
{
    public static bool CanAdminister(ClaimsPrincipal user, Event @event)
    {
        List<int> administeredPageIds = user.ParseAdministeredPageIds();
        return CanEditContent(administeredPageIds, @event);
    }

    public static bool CanEditContent(ClaimsPrincipal user, Post post)
    {
        return false;
    }

    public static bool CanMove()
    {
        return false;
    }

    public static bool CanEditContent(List<int> administeredByUserPageIds, Event @event)
    {
        List<int> eventsPageIds = @event.Categories.Select(c => c.PageId).ToList();
        if (administeredByUserPageIds.Intersect(eventsPageIds).Any())
            return true;
        if (@event.Parent is { } parentEvent && CanEditContent(administeredByUserPageIds, parentEvent))
            return true;
        return false;
    }

    private static List<int> ParseAdministeredPageIds(this ClaimsPrincipal user)
    {
        return user.Claims
            .Where(c => c.Type == Constants.StartSchPageAdminClaim)
            .Select(c => int.Parse(c.Value))
            .ToList();
    }
}

public class AdministrationAuthorizationService(IHttpContextAccessor httpContextAccessor)
{
    public FrozenSet<int> AdministeredPageIds => field ??=
        httpContextAccessor.HttpContext!.User.Claims
            .Where(c => c.Type == Constants.StartSchPageAdminClaim)
            .Select(c => int.Parse(c.Value))
            .ToFrozenSet();

    public bool CanAdministerExisting(IEventNode node)
    {
        return node.Categories.Any(category => AdministeredPageIds.Contains(category.PageId));
    }
}

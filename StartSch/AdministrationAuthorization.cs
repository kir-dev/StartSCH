using System.Security.Claims;
using StartSch.Data;

namespace StartSch;

// Requirements:
// - Events can have a parent
// - Posts can belong to an Event
// - Posts have Categories[1..*]
// - Events have Categories[1..*]
// - Every Category has a Page it belongs to
// - Users can administer Pages
// - A User can administer a Category if the User can administer the Category's Page
// - A User can edit an Event if the User can administer any of the Event's Categories
// - A User can edit a Post if the User can administer any of the Post's Categories
// - A User can create Events/Posts with Pages the User can administer
// - A User can create a CollaborationRequest to add Categories the User can't administer to Events/Pages
// - A User can create a sub-Event if the User can administer the parent Event and the sub-Event
// - A User can add a Category to an Event/Post if the User can administer the Event/Post and the Category
// - A User can remove a Category from an Event/Post if
//     - the User can administer the Event/Post,
// - Can associate Event/Post with Page if can administer Page and the Event/Post
// - Disassociate Event/Post from Page if can administer Event/Post
public static class AdministrationAuthorization
{
    public static bool CanEditContent(ClaimsPrincipal user, Event @event)
    {
        List<int> administeredPageIds = user.ParseAdministeredPageIds();
        return CanEditContent(administeredPageIds, @event);
    }

    public static bool CanEditContent(ClaimsPrincipal user, Post post)
    {
        
    }

    public static bool CanMove()
    {
        
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

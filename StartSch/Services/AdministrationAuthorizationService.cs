using System.Collections.Frozen;
using StartSch.Data;

namespace StartSch.Services;

public class AdministrationAuthorizationService(IHttpContextAccessor httpContextAccessor)
{
    public FrozenSet<int> AdministeredPageIds => field ??=
        httpContextAccessor.HttpContext!.User.Claims
            .Where(c => c.Type == Constants.StartSchPageAdminClaim)
            .Select(c => int.Parse(c.Value))
            .ToFrozenSet();

    private bool CanAdminister(Category category)
    {
        return AdministeredPageIds.Contains(category.PageId);
    }

    private bool CanAdministerAnyCategories(IEventNode node)
    {
        return node.Categories.Any(CanAdminister);
    }

    public void CheckCreate(IEventNode newNode)
    {
        var parent = newNode.Parent;
        var categories = newNode.Categories;
        Require(
            parent == null
            || CanAdministerAnyCategories(parent)
        );
        Require(
            categories.All(c =>
                CanAdminister(c)
                || parent != null && parent.Categories.Contains(c)
            )
        );
        Require(
            categories.Any(CanAdminister)
        );
    }

    public void CheckUpdate(IEventNode existingNode, IEventNode? newParent, List<Category> newCategories)
    {
        Require(
            CanAdministerAnyCategories(existingNode)
        );
        Require(
            existingNode.Parent == newParent
            || newParent == null // if we can administer the node, we can move it
            || CanAdministerAnyCategories(newParent)
        );
        Require(
            newCategories.All(c =>
                CanAdminister(c)
                || existingNode.Categories.Contains(c)
                || newParent != null && newParent.Categories.Contains(c)
            )
        );
        Require(
            newCategories.Any(CanAdminister)
        );
    }

    public bool CanEdit(IEventNode node)
    {
        return CanAdministerAnyCategories(node);
    }

    public bool CanCreateChildEvent(IEventNode node)
    {
        return CanAdministerAnyCategories(node);
    }

    public bool CanCreatePost(Event @event)
    {
        return CanAdministerAnyCategories(@event);
    }

    public bool CanAdministerAnyPages()
    {
        return AdministeredPageIds.Count > 0;
    }

    public bool CanCreateEvent(Page page)
    {
        return AdministeredPageIds.Contains(page.Id);
    }

    public bool CanCreatePost(Page page)
    {
        return AdministeredPageIds.Contains(page.Id);
    }

    private static void Require(bool assertion)
    {
        if (!assertion) throw new InvalidOperationException();
    }
}

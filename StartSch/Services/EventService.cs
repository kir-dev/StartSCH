using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StartSch.Authorization.Requirements;
using StartSch.Data;

namespace StartSch.Services;

public class EventService(
    Db db,
    AdministrationAuthorizationService administrationAuthorizationService,
    IAuthorizationService authorizationService)
{
    public async Task<Event> Save(
        ClaimsPrincipal user,
        int eventId,
        int? parentId,
        HashSet<int> categoryIds,
        string title,
        string? descriptionMd,
        Instant? start,
        Instant? endUtc)
    {
        if (categoryIds.Count == 0)
            throw new InvalidOperationException("Must have at least one category");
        
        Event @event;

        if (eventId == 0) // create new event
        {
            @event = new()
            {
                Title = title,
                Start = start,
                End = endUtc,
            };

            db.Events.Add(@event);

            List<Category> categories = await db.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync();

            Event? parentEvent = parentId.HasValue
                ? await db.Events
                    .Include(e => e.Categories)
                    .FirstAsync(e => e.Id == parentId)
                : null;

            if (parentEvent != null)
            {
                if (!administrationAuthorizationService.CanAdministerExisting(parentEvent))
                    throw new InvalidOperationException();

                // Allow inheriting parent's categories even if they aren't directly administrable by the user
                HashSet<Category> nonAdministrableCategories = categories
                    .Where(c => !administrationAuthorizationService.AdministeredPageIds.Contains(c.PageId))
                    .ToHashSet();
                if (!nonAdministrableCategories.All(c => parentEvent.Categories.Contains(c)))
                    throw new InvalidOperationException();

                @event.Parent = parentEvent;
            }
        }
        else // update existing event
        {
            @event = await db.Events
                         .Include(e => e.Parent)
                         .ThenInclude(e => e!.Categories)
                         .ThenInclude(c => c.Page)
                         .Include(e => e.Categories)
                         .ThenInclude(c => c.Page)
                         .FirstOrDefaultAsync(e => e.Id == eventId)
                     ?? throw new InvalidOperationException("Event not found");

            if (!administrationAuthorizationService.CanAdministerExisting(@event))
                throw new InvalidOperationException("Unauthorized to modify this Event");

            @event.Start = start;
            @event.End = endUtc;
            @event.Title = title;
            @event.DescriptionMarkdown = descriptionMd;
        }

        Event? newParent = parentId.HasValue
            ? await db.Events
                  .Include(e => e.Categories)
                  .ThenInclude(c => c.Page)
                  .FirstOrDefaultAsync(e => e.Id == parentId)
              ?? throw new InvalidOperationException("Parent Event not found")
            : null;

        List<Category> newCategories = await db.Categories
            .Include(c => c.Page)
            .Where(g => categoryIds.Contains(g.Id))
            .ToListAsync();
        if (newCategories.Count != categoryIds.Count)
            throw new InvalidOperationException("Categories not found");
        
        List<Page> oldOwners = @event.GetOwners();
        List<Page> newOwners = newCategories.Select(c => c.Page).Distinct().ToList();

        if (newParent == null)
        {
            // either only have a single owner or all new owners must already own the event
            bool isValid = newOwners.Count == 1 || newOwners.All(p => oldOwners.Contains(p));
            if (!isValid) throw new InvalidOperationException();
        }
        else
        {
            List<Page> newParentOwners = newParent.GetOwners();
            
            // every owner must already own the parent or the event
            bool isValid = newOwners.All(g => newParentOwners.Contains(g) || oldOwners.Contains(g));
            if (!isValid) throw new InvalidOperationException();
        }
        @event.Categories.Clear();
        @event.Categories.AddRange(newCategories);

        if (@event.ParentId != parentId)
        {
            if (parentId.HasValue)
            {
                // must have access to new parent to add to it
                var canAddToNewParent = await authorizationService.AuthorizeAsync(
                    user, newParent, ResourceAccessRequirement.Write);
                if (!canAddToNewParent.Succeeded) throw new InvalidOperationException();
            }
            // can remove from parent regardless of access to parent. the last authorization call checks
            // whether the user still has access to the event without the old parent

            @event.Parent = newParent;
        }

        var canSave = await authorizationService.AuthorizeAsync(user, @event, ResourceAccessRequirement.Write);
        if (!canSave.Succeeded) throw new InvalidOperationException();

        await db.SaveChangesAsync();

        return @event;
    }
}

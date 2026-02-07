using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class EventService(
    Db db,
    AdministrationAuthorizationService administrationAuthorizationService
)
{
    public async Task<Event> Save(
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

        if (eventId == 0) // Create new event
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


            if (parentId.HasValue)
            {
                Event parentEvent = await db.Events
                    .Include(e => e.Categories)
                    .FirstAsync(e => e.Id == parentId);
                
                if (
                    // Require at least one administrable category on the parent event
                    !administrationAuthorizationService.CanAdministerExisting(parentEvent)
                    // Require at least one administrable category on the event
                    || !categories.Any(c => administrationAuthorizationService.AdministeredPageIds.Contains(c.PageId))
                )
                    throw new InvalidOperationException();

                // Allow inheriting parent's categories even if they aren't directly administrable by the user
                HashSet<Category> nonAdministrableCategories = categories
                    .Where(c => !administrationAuthorizationService.AdministeredPageIds.Contains(c.PageId))
                    .ToHashSet();
                if (!nonAdministrableCategories.All(c => parentEvent.Categories.Contains(c)))
                    throw new InvalidOperationException();

                @event.Parent = parentEvent;
            }
            else
            {
                // Require all categories to be administrable
                if (!categories.All(c => administrationAuthorizationService.AdministeredPageIds.Contains(c.PageId)))
                    throw new InvalidOperationException();
            }
            
            @event.Categories.AddRange(categories);
        }
        else // Update existing event
        {
            @event = await db.Events
                .Include(e => e.Parent)
                .ThenInclude(e => e!.Categories)
                .Include(e => e.Categories)
                .FirstAsync(e => e.Id == eventId);

            if (!administrationAuthorizationService.CanAdministerExisting(@event))
                throw new InvalidOperationException("Unauthorized to modify this Event");

            if (parentId != @event.ParentId)
            {
                if (parentId == null)
                {
                    @event.Parent = null;
                }
                else
                {
                    var newParent = await db.Events
                        .Include(e => e.Categories)
                        .FirstAsync(e => e.Id == parentId);
                    if (!administrationAuthorizationService.CanAdministerExisting(newParent))
                        throw new InvalidOperationException();
                    @event.ParentId = parentId;
                    @event.Parent = newParent;
                }
            }

            var parentCategories = @event.Parent?.Categories ?? [];
            var currentCategories = @event.Categories;

            var categories = await db.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync();
            if (
                // Check that all categories are valid
                !categories.All(c =>
                    administrationAuthorizationService.AdministeredPageIds.Contains(c.PageId)
                    || currentCategories.Contains(c)
                    || parentCategories.Contains(c)
                )
                // Require at least one administrable category
                || !categories.Any(c => administrationAuthorizationService.AdministeredPageIds.Contains(c.PageId))
            )
                throw new InvalidOperationException();

            @event.Categories.Clear();
            @event.Categories.AddRange(categories);

            @event.Start = start;
            @event.End = endUtc;
            @event.Title = title;
            @event.DescriptionMarkdown = descriptionMd;
        }

        await db.SaveChangesAsync();

        return @event;
    }
}

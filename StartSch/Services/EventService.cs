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
        Instant? end)
    {
        Event @event;

        if (eventId == 0) // Create a new event
        {
            @event = new()
            {
                Title = title,
                DescriptionMarkdown = descriptionMd,
                Start = start,
                End = end,
                Parent = parentId.HasValue
                    ? await db.Events
                        .Include(e => e.Categories)
                        .Where(e => e.Id == parentId)
                        .FirstAsync()
                    : null,
            };

            @event.Categories.AddRange(
                await db.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToListAsync()
            );
            
            administrationAuthorizationService.CheckCreate(@event);

            db.Events.Add(@event);
        }
        else // Update existing event
        {
            @event = await db.Events
                .Include(e => e.Parent)
                .ThenInclude(e => e!.Categories)
                .Include(e => e.Categories)
                .FirstAsync(e => e.Id == eventId);

            var newParent = parentId == null
                ? null
                : @event.Parent is { } parent && parent.Id == parentId
                    ? parent
                    : await db.Events
                        .Include(e => e.Categories)
                        .FirstAsync(e => e.Id == parentId);
            var newCategories = await db.Categories
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync();
            
            administrationAuthorizationService.CheckUpdate(@event, newParent, newCategories);
            
            @event.Categories.Clear();
            @event.Categories.AddRange(newCategories);

            @event.Start = start;
            @event.End = end;
            @event.Title = title;
            @event.DescriptionMarkdown = descriptionMd;
        }

        await db.SaveChangesAsync();

        return @event;
    }
}

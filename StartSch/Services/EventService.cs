using Microsoft.EntityFrameworkCore;
using StartSch.Data;

namespace StartSch.Services;

public class EventService(
    Db db,
    AuthorizationService authorizationService
)
{
    public async Task<Event> Save(
        int eventId,
        int? parentId,
        HashSet<int> categoryIds,
        HashSet<int> possibleCollaborationRequestPageIds,
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

            authorizationService.CheckCreate(@event);

            db.Events.Add(@event);
            
            // Save collaboration
            if (possibleCollaborationRequestPageIds.Count > 0)
            {
                // Looking up the pages might not be needed, ALBI shall decide
                var pages = await db.Pages
                    .Where(p => possibleCollaborationRequestPageIds.Contains(p.Id))
                    .ToListAsync();

                var collaborationRequests = possibleCollaborationRequestPageIds
                    .Select(pageId => new EventCollaborationRequest
                    {
                        PageId = pageId,
                        Page = pages.First(p => p.Id == pageId),
                        Event = @event,
                        EventId = @event.Id
                    })
                    .ToList();

                db.EventCollaborationRequests.AddRange(collaborationRequests);
            }

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

            authorizationService.CheckUpdate(@event, newParent, newCategories);

            @event.Parent = newParent;
            @event.Categories.Clear();
            @event.Categories.AddRange(newCategories);

            @event.Start = start;
            @event.End = end;
            @event.Title = title;
            @event.DescriptionMarkdown = descriptionMd;
            
            // Update collaboration requests
            var existingCollaborationRequests = await db.EventCollaborationRequests
                .Where(ecr => ecr.EventId == eventId)
                .ToListAsync();

            db.EventCollaborationRequests.RemoveRange(existingCollaborationRequests);

            if (possibleCollaborationRequestPageIds.Count > 0)
            {
                var collaborationRequests = possibleCollaborationRequestPageIds
                    .Select(pageId => new EventCollaborationRequest
                    {
                        PageId = pageId,
                        Event = @event,
                        EventId = @event.Id
                    })
                    .ToList();

                db.EventCollaborationRequests.AddRange(collaborationRequests);
            }
        }

        await db.SaveChangesAsync();

        return @event;
    }
}

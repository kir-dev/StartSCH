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
        string title,
        string? descriptionMd,
        Instant? start,
        Instant? end)
    {
        Event @event;
        var administeredPageIds = authorizationService.AdministeredPageIds;
        var selectedCategories = await db.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync();

        if (eventId == 0) // Create a new event
        {
            var newParent = parentId.HasValue
                ? await db.Events
                    .Include(e => e.Categories)
                    .Where(e => e.Id == parentId)
                    .FirstAsync()
                : null;

            @event = new()
            {
                Title = title,
                DescriptionMarkdown = descriptionMd,
                Start = start,
                End = end,
                Parent = newParent,
            };

            var assignableCategories = selectedCategories
                .Where(c => administeredPageIds.Contains(c.PageId)
                            || (newParent != null && newParent.Categories.Any(pc => pc.Id == c.Id)))
                .ToList();

            @event.Categories.AddRange(assignableCategories);
            authorizationService.CheckCreate(@event);

            db.Events.Add(@event);

            var possibleCollaborationRequestPageIds = selectedCategories
                .Select(c => c.PageId)
                .Except(assignableCategories.Select(c => c.PageId))
                .Where(pageId => !administeredPageIds.Contains(pageId))
                .Distinct()
                .ToList();

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
        else // Update existing event
        {
            @event = await db.Events
                .Include(e => e.Parent)
                .ThenInclude(e => e!.Categories)
                .Include(e => e.Categories)
                .FirstAsync(e => e.Id == eventId);

            var existingCategoryIds = @event.Categories.Select(c => c.Id).ToHashSet();

            var newParent = parentId == null
                ? null
                : @event.Parent is { } parent && parent.Id == parentId
                    ? parent
                    : await db.Events
                        .Include(e => e.Categories)
                        .FirstAsync(e => e.Id == parentId);

            var assignableCategories = selectedCategories
                .Where(c => administeredPageIds.Contains(c.PageId)
                            || existingCategoryIds.Contains(c.Id)
                            || (newParent != null && newParent.Categories.Any(pc => pc.Id == c.Id)))
                .ToList();

            authorizationService.CheckUpdate(@event, newParent, assignableCategories);

            @event.Parent = newParent;
            @event.Categories.Clear();
            @event.Categories.AddRange(assignableCategories);

            @event.Start = start;
            @event.End = end;
            @event.Title = title;
            @event.DescriptionMarkdown = descriptionMd;

            var existingCollaborationRequests = await db.EventCollaborationRequests
                .Where(ecr => ecr.EventId == eventId)
                .ToListAsync();
            db.EventCollaborationRequests.RemoveRange(existingCollaborationRequests);

            var possibleCollaborationRequestPageIds = selectedCategories
                .Select(c => c.PageId)
                .Except(assignableCategories.Select(c => c.PageId))
                .Where(pageId => !administeredPageIds.Contains(pageId))
                .Distinct()
                .ToList();

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

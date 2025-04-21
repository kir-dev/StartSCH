using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using StartSch.Authorization.Requirements;
using StartSch.Data;

namespace StartSch.Services;

public class EventService(
    Db db,
    IAuthorizationService authorizationService)
{
    public async Task<Event> Save(
        ClaimsPrincipal user,
        int eventId,
        int? parentId,
        List<int> categoryIds,
        string title,
        string? descriptionMd,
        DateTime startUtc,
        DateTime endUtc)
    {
        Event @event;

        if (eventId == 0) // create new event
        {
            @event = new()
            {
                Title = title,
                CreatedUtc = DateTime.UtcNow,
                StartUtc = startUtc,
                EndUtc = endUtc,
            };

            db.Events.Add(@event);
        }
        else // update existing event
        {
            @event = await db.Events
                         .Include(e => e.Parent)
                         .Include(e => e.Groups)
                         .FirstOrDefaultAsync(e => e.Id == eventId)
                     ?? throw new InvalidOperationException();

            var canUpdate = await authorizationService.AuthorizeAsync(user, @event, ResourceAccessRequirement.Write);
            if (!canUpdate.Succeeded) throw new InvalidOperationException();

            @event.StartUtc = startUtc;
            @event.EndUtc = endUtc;
            @event.Title = title;
            @event.DescriptionMarkdown = descriptionMd;
        }

        Event? newParent = parentId.HasValue
            ? await db.Events
                  .Include(e => e.Groups)
                  .FirstOrDefaultAsync(e => e.Id == parentId)
              ?? throw new InvalidOperationException()
            : null;

        List<Page> newGroups = await db.Pages
            .Where(g => categoryIds.Contains(g.Id))
            .ToListAsync();
        if (newGroups.Count == 0) throw new InvalidOperationException();
        if (newParent == null)
        {
            // either only have a single group or all groups must already have access to the event
            bool isValid = newGroups.Count == 1 || newGroups.All(g => @event.Groups.Contains(g));
            if (!isValid) throw new InvalidOperationException();
        }
        else
        {
            // every group must already have access to the parent or the event
            bool isValid = newGroups.All(g => newParent.Groups.Contains(g) || @event.Groups.Contains(g));
            if (!isValid) throw new InvalidOperationException();
        }
        @event.Groups.Clear();
        @event.Groups.AddRange(newGroups);

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

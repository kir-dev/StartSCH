using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Services;

namespace StartSch.Controllers;

[ApiController, Authorize]
// blazor makes csrf protection a pain. if you don't want csrf, update your browser
public class InterestSubscriptionController(Db db, IMemoryCache cache) : ControllerBase
{
    [HttpPut("/api/interests/{interestId:int}/subscriptions")]
    public async Task<IActionResult> Subscribe(int interestId)
    {
        int userId = User.GetId();
        db.InterestSubscriptions.Add(new() { UserId = userId, InterestId = interestId });
        try
        {
            await db.SaveChangesAsync();
            cache.Remove(InterestService.UserSubscriptionsCacheKeyPrefix + userId);
            return Created();
        }
        catch (DbUpdateException)
        {
            return NoContent();
        }
    }

    [HttpDelete("/api/interests/{interestId:int}/subscriptions")]
    public async Task<IActionResult> Unsubscribe(int interestId)
    {
        int userId = User.GetId();
        int rows = await db.InterestSubscriptions
            .Where(s => s.UserId == userId && s.InterestId == interestId)
            .ExecuteDeleteAsync();
        if (rows > 0)
        {
            cache.Remove(InterestService.UserSubscriptionsCacheKeyPrefix + userId);
        }

        return NoContent();
    }
}

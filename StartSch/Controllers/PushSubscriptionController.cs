using System.Web;
using Lib.AspNetCore.WebPush;
using Lib.Net.Http.WebPush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StartSch.Auth;
using StartSch.Data;
using PushSubscription = StartSch.Data.PushSubscription;

namespace StartSch.Controllers;

[ApiController]
[Route("/api/push-subscriptions")]
public class PushSubscriptionController(
    Db db,
    IOptions<PushServiceClientOptions> pushOptions,
    IMemoryCache cache
) : ControllerBase
{
    [HttpPut, Authorize]
    // no csrf validation needed as this implicitly only accepts Content-Type: application/json
    public async Task<IActionResult> Put([FromBody] Lib.Net.Http.WebPush.PushSubscription subscription)
    {
        Guid? userId = User.GetAuthSchId();
        if (!userId.HasValue) return Unauthorized();
        User user = await db.Users
                        .Include(u => u.PushSubscriptions)
                        .FirstOrDefaultAsync(u => u.Id == userId)
                    ?? db.Users.Add(new() { Id = userId.Value }).Entity;
        if (user.PushSubscriptions.Any(s => s.Endpoint == subscription.Endpoint))
            return NoContent();
        user.PushSubscriptions.Add(new()
        {
            Endpoint = subscription.Endpoint,
            P256DH = subscription.GetKey(PushEncryptionKeyName.P256DH),
            Auth = subscription.GetKey(PushEncryptionKeyName.Auth),
        });
        await db.SaveChangesAsync();
        cache.Remove(nameof(PushSubscriptionState) + userId.Value);
        return Created();
    }

    [HttpDelete("{endpoint}")]
    public async Task<IActionResult> Delete(string endpoint)
    {
        endpoint = HttpUtility.UrlDecode(endpoint);

        PushSubscription? subscription = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (subscription == null)
            return NotFound();

        db.PushSubscriptions.Remove(subscription);
        await db.SaveChangesAsync();
        cache.Remove(nameof(PushSubscriptionState) + subscription.UserId);

        return NoContent();
    }

    [HttpGet("public-key")]
    public IActionResult GetVapidPublicKey() => Ok(pushOptions.Value.PublicKey);
}
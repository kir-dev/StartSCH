using System.Web;
using Lib.AspNetCore.WebPush;
using Lib.Net.Http.WebPush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
    public async Task<IActionResult> Put([FromBody] Lib.Net.Http.WebPush.PushSubscription dto)
    {
        Guid userId = User.GetAuthSchId()!.Value;

        PushSubscription? subscription = await db.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint);

        if (subscription != null && subscription.UserId != userId)
        {
            Guid oldUserId = subscription.UserId;
            subscription.UserId = userId;
            await db.SaveChangesAsync();
            cache.Remove(nameof(PushSubscriptionState) + oldUserId);
            cache.Remove(nameof(PushSubscriptionState) + userId);
            return NoContent();
        }

        db.PushSubscriptions.Add(new()
        {
            UserId = userId,
            Endpoint = dto.Endpoint,
            P256DH = dto.GetKey(PushEncryptionKeyName.P256DH),
            Auth = dto.GetKey(PushEncryptionKeyName.Auth),
        });

        await db.SaveChangesAsync();
        cache.Remove(nameof(PushSubscriptionState) + userId);
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
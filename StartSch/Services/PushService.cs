using System.Web;
using Lib.AspNetCore.WebPush;
using Lib.Net.Http.WebPush;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StartSch.Auth;
using StartSch.Data;
using StartSch.Wasm;
using PushSubscription = StartSch.Data.PushSubscription;

namespace StartSch.Services;

public class PushService(Db db, PushServiceClient pushServiceClient, IMemoryCache cache)
{
    public async Task SendNotification(PushNotification message, IEnumerable<string> tags)
    {
        var targets = TagGroup.GetAllTargets(tags);
        var subscriptions = await db.PushSubscriptions
            .Where(s => s.User.Tags.Any(t => targets.Contains(t.Path)))
            .AsNoTracking()
            .ToListAsync();
        foreach (PushSubscription subscription in subscriptions)
        {
            Lib.Net.Http.WebPush.PushSubscription pushSubscription = new() { Endpoint = subscription.Endpoint };
            pushSubscription.SetKey(PushEncryptionKeyName.Auth, subscription.Auth);
            pushSubscription.SetKey(PushEncryptionKeyName.P256DH, subscription.P256DH);
            try
            {
                await pushServiceClient.RequestPushMessageDeliveryAsync(
                    pushSubscription,
                    new(JsonContent.Create(message))
                );
            }
            catch (PushServiceClientException e)
            {
                // The push service for Firefox Android returns 200 OK instead of 201 Created because why wouldn't it
                // https://datatracker.ietf.org/doc/html/rfc8030#section-5
                if (e.Message == "OK") continue;

                db.PushSubscriptions.Remove(subscription);
                await db.SaveChangesAsync();
                cache.Remove(nameof(PushSubscriptionState) + subscription.UserId);
            }
        }
    }
}

[ApiController]
[Route("/api/push-subscriptions")]
public class PushSubscriptionController(
    Db db,
    IOptions<PushServiceClientOptions> pushOptions,
    IMemoryCache cache
) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Lib.Net.Http.WebPush.PushSubscription subscription)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        Guid? userId = User.GetAuthSchId();
        if (!userId.HasValue) return Unauthorized();
        User user = await db.Users
                        .Include(u => u.PushSubscriptions)
                        .FirstOrDefaultAsync(u => u.Id == userId)
                    ?? db.Users.Add(new() { Id = userId.Value }).Entity;
        if (user.PushSubscriptions.Any(s => s.Endpoint == subscription.Endpoint))
            return Ok();
        user.PushSubscriptions.Add(new()
        {
            Endpoint = subscription.Endpoint,
            P256DH = subscription.GetKey(PushEncryptionKeyName.P256DH),
            Auth = subscription.GetKey(PushEncryptionKeyName.Auth),
        });
        await db.SaveChangesAsync();
        cache.Remove(nameof(PushSubscriptionState) + userId.Value);
        return Ok();
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

        return Ok();
    }

    [HttpGet("public-key")]
    public IActionResult GetVapidPublicKey() => Ok(pushOptions.Value.PublicKey);
}
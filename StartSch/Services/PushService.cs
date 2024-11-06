using System.Web;
using Lib.AspNetCore.WebPush;
using Lib.Net.Http.WebPush;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StartSch.Auth;
using StartSch.Data;

namespace StartSch.Services;

public class PushService(Db db, PushServiceClient pushServiceClient)
{
    public async Task SendNotification(string title, string body, string? url)
    {
    }
}

[ApiController]
[Route("/api/push-subscriptions")]
public class PushSubscriptionController(Db db, IOptions<PushServiceClientOptions> pushOptions) : ControllerBase
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
        return Ok();
    }

    [HttpDelete("{endpoint}")]
    [Authorize]
    public async Task<IActionResult> Delete(string endpoint)
    {
        Guid? userId = User.GetAuthSchId();
        if (!userId.HasValue) return Unauthorized();

        endpoint = HttpUtility.UrlDecode(endpoint);

        await db.PushSubscriptions
            .Where(s => s.Endpoint == endpoint && s.UserId == userId.Value)
            .ExecuteDeleteAsync();

        return Ok();
    }

    [HttpGet("public-key")]
    public IActionResult GetVapidPublicKey() => Ok(pushOptions.Value.PublicKey);
}
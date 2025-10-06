using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Wasm;

namespace StartSch.Services;

public class PushSubscriptionService(IDbContextFactory<Db> dbFactory, IMemoryCache cache)
{
    public static string GetPushEndpointsCacheKey(int userId) => "PushEndpointHashes" + userId;

    public async Task<string> GetPushEndpointHashes(ClaimsPrincipal user)
    {
        var userId = user.GetId();
        return (await cache.GetOrCreateAsync(
            GetPushEndpointsCacheKey(userId),
            async _ =>
            {
                await using var db = await dbFactory.CreateDbContextAsync();
                var subscriptionEndpointHashes = await db.PushSubscriptions
                    .AsNoTracking()
                    .Where(s => s.UserId == userId)
                    .Select(s => SharedUtils.ComputeSha256(s.Endpoint))
                    .ToListAsync();
                return JsonSerializer.Serialize(subscriptionEndpointHashes, JsonSerializerOptions.Web);
            }
        ))!;
    }
}

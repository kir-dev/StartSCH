using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StartSch.Data;
using StartSch.Modules.SchPincer;

namespace StartSch.Services;

public class UserInfoService(Db db, IMemoryCache cache)
{
    public async Task OnUserInformationReceived(UserInformationReceivedContext context)
    {
        Guid userId = context.Principal!.GetAuthSchId()!.Value;
        User user = await db.Users
                        .FirstOrDefaultAsync(u => u.Id == userId)
                    ?? db.Users.Add(new() { Id = userId }).Entity;

        AuthSchUserInfo userInfo =
            context.User.Deserialize<AuthSchUserInfo>(Utils.JsonSerializerOptionsWeb)!;

        user.AuthSchEmail = userInfo.EmailVerified ? userInfo.Email : null;

        // add claims to the user's cookie
        ClaimsIdentity identity = (ClaimsIdentity)context.Principal!.Identity!;
        if (userInfo.PekActiveMemberships != null)
        {
            identity.AddClaim(new(
                "memberships",
                JsonSerializer.Serialize(
                    userInfo.PekActiveMemberships?
                        .Select(m => new GroupMembership(m.PekId, m.Name, m.Title))
                        .ToList())));
        }

        // update groups in db
        if (userInfo.PekActiveMemberships != null)
        {
            var groupIds = userInfo.PekActiveMemberships
                .Select(m => (int?)m.PekId)
                .ToList();
            List<Page> groups = await db.Pages
                .Where(g => groupIds.Contains(g.PekId))
                .ToListAsync();
            Dictionary<int, AuthSchActiveMembership> memberships = userInfo.PekActiveMemberships!
                .ToDictionary(m => m.PekId);
            foreach (Page group in groups)
                if (memberships.Remove(group.PekId!.Value, out AuthSchActiveMembership? membership))
                    group.PekName = membership.Name;

            if (memberships.Count != 0)
            {
                // check for pincer groups with no pek id
                List<Page> pincerGroups = await db.Pages
                    .Where(g => g.PincerName != null && g.PekId == null)
                    .ToListAsync();
                foreach (AuthSchActiveMembership membership in memberships.Values)
                {
                    List<Page> candidates = pincerGroups
                        .Where(g => g.PincerName!.RoughlyMatches(membership.Name))
                        .ToList();

                    switch (candidates.Count)
                    {
                        case > 1:
                            throw new($"Multiple candidates for {membership.Name}");
                        case 1:
                            candidates[0].PekId = membership.PekId;
                            candidates[0].PekName = membership.Name;
                            memberships.Remove(membership.PekId);
                            break;
                        default:
                            db.Pages.Add(new() { PekId = membership.PekId, PekName = membership.Name });
                            break;
                    }
                }
            }
        }

        int updates = await db.SaveChangesAsync();
        if (updates > 0)
            cache.Remove(SchPincerModule.PincerGroupsCacheKey);
    }
}

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
    // TODO: add retrying to OnUserInformationReceived
    public async Task OnUserInformationReceived(UserInformationReceivedContext context)
    {
        Guid authSchId = context.Principal!.GetAuthSchId()!.Value;
        User user = await db.Users
                        .FirstOrDefaultAsync(u => u.AuthSchId == authSchId)
                    ?? db.Users.Add(new() { AuthSchId = authSchId }).Entity;

        AuthSchUserInfo userInfo = context.User.Deserialize<AuthSchUserInfo>(JsonSerializerOptions.Web)!;

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

        // update pages in db
        if (userInfo.PekActiveMemberships != null)
        {
            var pekIds = userInfo.PekActiveMemberships
                .Select(m => (int?)m.PekId)
                .ToList();
            List<Page> pages = await db.Pages
                .Where(g => pekIds.Contains(g.PekId))
                .ToListAsync();
            Dictionary<int, AuthSchActiveMembership> memberships = userInfo.PekActiveMemberships!
                .ToDictionary(m => m.PekId);
            
            // update existing pages
            foreach (Page page in pages)
            {
                memberships.Remove(page.PekId!.Value, out AuthSchActiveMembership? membership);
                page.PekName = membership!.Name;
            }

            // create new pages from the remaining memberships
            foreach (var membership in memberships.Values)
                db.Pages.Add(new() { PekId = membership.PekId, PekName = membership.Name });
        }

        int updates = await db.SaveChangesAsync();
        if (updates > 0)
            cache.Remove(SchPincerModule.PincerPagesCacheKey);
        
        identity.AddClaim(new("id", user.Id.ToString()));
    }
}

using System.Globalization;
using System.Runtime.InteropServices;
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
        Guid authSchId = context.Principal!.GetAuthSchId()!.Value;
        User user = await db.Users
                        .FirstOrDefaultAsync(u => u.AuthSchId == authSchId)
                    ?? db.Users.Add(new() { AuthSchId = authSchId }).Entity;

        AuthSchUserInfo userInfo = context.User.Deserialize<AuthSchUserInfo>(Utils.JsonSerializerOptions)!;

        user.AuthSchEmail = userInfo.EmailVerified ? userInfo.Email : null;

        // add claims to the user's cookie
        ClaimsIdentity identity = (ClaimsIdentity)context.Principal!.Identity!;

        List<Page> administeredPages = [];

        // update pages in db
        if (userInfo.PekActiveMemberships != null)
        {
            var memberships = userInfo.PekActiveMemberships;
            
            // TODO: REMOVE
            // TODO: REMOVE
            memberships.Add(new(473, "LángoSCH", ["admin"]));
            // TODO: REMOVE
            // TODO: REMOVE

            var pekGroupIds = memberships
                .Select(m => m.PekId)
                .ToList();
            var administeredPekGroupIds = memberships
                .Where(m => m.Titles.Any(Constants.IsPrivilegedPekTitle))
                .Select(m => m.PekId)
                .ToHashSet();
            Dictionary<int, Page> pekGroupIdToPage = await db.Pages
                .Where(g => pekGroupIds.Contains(g.PekId!.Value))
                .ToDictionaryAsync(p => p.PekId!.Value);

            foreach (var membership in memberships)
            {
                ref var page = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    pekGroupIdToPage,
                    membership.PekId, out bool _
                );
                page ??= db.Pages.Add(new()
                {
                    PekId = membership.PekId,
                    Categories =
                    {
                        new()
                        {
                            Interests =
                            {
                                new EmailWhenPostPublishedInCategory(),
                                new PushWhenPostPublishedInCategory(),
                                new ShowEventsInCategory(),
                                new ShowPostsInCategory(),
                            }
                        }
                    }
                }).Entity;
                page.PekName = membership.Name;
            }

            administeredPages = administeredPekGroupIds.Select(x => pekGroupIdToPage[x]).ToList();
        }

        db.SetCreatedAndUpdatedTimestamps(e => e switch
        {
            User or Page => TimestampUpdateFlags.CreatedUpdated,
            _ => TimestampUpdateFlags.None
        });

        int updates = await db.SaveChangesAsync();
        if (updates > 0)
            cache.Remove(SchPincerModule.PincerPagesCacheKey);

        identity.AddClaim(new(
            Constants.StartSchUserIdClaim,
            user.Id.ToString(CultureInfo.InvariantCulture)
        ));
        identity.AddClaims(administeredPages
            .Select(p => new Claim(
                Constants.StartSchPageAdminClaim,
                p.Id.ToString(CultureInfo.InvariantCulture)
            ))
        );
    }
}

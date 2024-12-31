using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using StartSch.Auth.Requirements;
using StartSch.Data;

namespace StartSch.Auth;

// Blazor OIDC sample: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-8.0&pivots=without-bff-pattern
// Issue: no OAuth2 refresh token support in ASP.NET https://github.com/dotnet/aspnetcore/issues/8175
// OIDC token refresh library: https://docs.duendesoftware.com/foss/accesstokenmanagement/web_apps/
public static class AuthSchSetup
{
    public static void AddAuthSch(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = "cookie";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("cookie", options =>
            {
                options.Cookie.Name = "web";
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = "https://auth.sch.bme.hu";

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("offline_access");
                options.Scope.Add("pek.sch.bme.hu:profile");
                // To retrieve a claim only available through the AuthSCH user info endpoint
                // (https://git.sch.bme.hu/kszk/authsch/-/wikis/api#a-userinfo-endpoint),
                // add its corresponding scope here, then map the claim in the OnUserInformationReceived
                // event handler below.

                options.ResponseType = "code";
                options.ResponseMode = "query";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false;
                options.SaveTokens = true;
                options.TokenValidationParameters.NameClaimType = "name";
                options.TokenValidationParameters.RoleClaimType = "roles";
                options.Backchannel = new()
                {
                    DefaultRequestHeaders = { { "User-Agent", "StartSCH/1 (https://start.alb1.hu)" } },
                    Timeout = options.BackchannelTimeout,
                    MaxResponseContentBufferSize = 10485760L
                };
            });

        // Add claims from the user info endpoint to the user's cookie and
        // yoink group info from PéK
        services.AddOptions<OpenIdConnectOptions>("oidc")
            .Configure(((OpenIdConnectOptions options, IServiceProvider serviceProvider) =>
            {
                // ran after querying the user info endpoint after logging in
                options.Events.OnUserInformationReceived = async context =>
                {
                    await using var serviceScope = serviceProvider.CreateAsyncScope();
                    Db db = serviceScope.ServiceProvider.GetRequiredService<Db>();
                    Guid userId = context.Principal!.GetAuthSchId()!.Value;

                    if (!await db.Users.AnyAsync(u => u.Id == userId))
                        db.Users.Add(new() { Id = userId });

                    AuthSchUserInfo userInfo =
                        context.User.Deserialize<AuthSchUserInfo>(Utils.JsonSerializerOptionsWeb)!;

                    // add claims to the user's cookie
                    ClaimsIdentity identity = (ClaimsIdentity)context.Principal!.Identity!;
                    if (userInfo.PekActiveMemberships != null)
                    {
                        userInfo.PekActiveMemberships.Add(new(528, "Paschta", ["adminsitrator"]));
                        userInfo.PekActiveMemberships.Add(new(473, "LángoSCH", ["uwu", "korvez"]));
                        userInfo.PekActiveMemberships.Add(new(490, "ReggeliSCH", ["xd"]));
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
                        List<Group> groups = await db.Groups
                            .Where(g => groupIds.Contains(g.PekId))
                            .ToListAsync();
                        Dictionary<int, AuthSchActiveMembership> memberships = userInfo.PekActiveMemberships!
                            .ToDictionary(m => m.PekId);
                        foreach (Group group in groups)
                            if (memberships.Remove(group.PekId!.Value, out AuthSchActiveMembership? membership))
                                group.PekName = membership.Name;

                        if (memberships.Count != 0)
                        {
                            // check for pincer groups with no pek id
                            List<Group> pincerGroups = await db.Groups
                                .Where(g => g.PincerName != null && g.PekId == null)
                                .ToListAsync();
                            foreach (AuthSchActiveMembership membership in memberships.Values)
                            {
                                List<Group> candidates = pincerGroups
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
                                        db.Groups.Add(new() { PekId = membership.PekId, PekName = membership.Name });
                                        break;
                                }
                            }
                        }
                    }

                    await db.SaveChangesAsync();
                };
            }));

        services.AddDistributedMemoryCache(); // needed by the token refresher
        services.AddOpenIdConnectAccessTokenManagement(); // the token refresher
        services.AddAuthorizationBuilder()
            .AddPolicy("Admin", policy => policy.AddRequirements(new AdminRequirement()))
            .AddPolicy("PostWrite", policy => policy.AddRequirements(new PostWriteRequirement()));
        services.AddCascadingAuthenticationState();
    }

    public static Guid? GetAuthSchId(this ClaimsPrincipal claimsPrincipal)
    {
        string? value = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (value != null)
            return Guid.Parse(value);
        return null;
    }
}
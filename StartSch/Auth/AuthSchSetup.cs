using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using StartSch.Data;

namespace StartSch.Auth;

// Based on https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-8.0&pivots=without-bff-pattern
public static class AuthSchSetup
{
    public static void AddAuthSch(this IServiceCollection services)
    {
        services.AddAuthentication(Constants.AuthSchAuthenticationScheme)
            .AddOpenIdConnect(Constants.AuthSchAuthenticationScheme, oidcOptions =>
            {
                oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                oidcOptions.Authority = "https://auth.sch.bme.hu/";
                oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
                oidcOptions.MapInboundClaims = false;
                oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                oidcOptions.TokenValidationParameters.RoleClaimType = "roles";
                oidcOptions.SaveTokens = true;
                oidcOptions.Scope.Add("offline_access");
                oidcOptions.Scope.Remove("profile");

                // To retrieve a claim only available through the AuthSCH user info endpoint,
                // (https://git.sch.bme.hu/kszk/authsch/-/wikis/api#a-userinfo-endpoint)
                // enable the following option and add a mapping:
                oidcOptions.GetClaimsFromUserInfoEndpoint = true;
                oidcOptions.Scope.Add("pek.sch.bme.hu:profile");
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        services.AddOptions<OpenIdConnectOptions>(Constants.AuthSchAuthenticationScheme)
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

                    AuthSchUserInfo userInfo = context.User.Deserialize<AuthSchUserInfo>(Utils.JsonSerializerOptionsWeb)!;

                    // add claims
                    ClaimsIdentity identity = (ClaimsIdentity)context.Principal!.Identity!;
                    identity.AddClaim(new("pekActiveMemberships", string.Join(',',
                        userInfo.PekActiveMemberships!.Select(m => m.PekId.ToString()))));

                    // add unknown groups and update group names
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
                        foreach (AuthSchActiveMembership membership in memberships.Values)
                            db.Groups.Add(new() { PekId = membership.PekId, PekName = membership.Name });
                    }

                    await db.SaveChangesAsync();
                };
            }));
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        services.AddSingleton<CookieOidcRefresher>();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure((CookieAuthenticationOptions cookieOptions, CookieOidcRefresher refresher) =>
            {
                cookieOptions.Events.OnValidatePrincipal = context =>
                    refresher.ValidateOrRefreshCookieAsync(context, Constants.AuthSchAuthenticationScheme);
            });
    }

    public static Guid? GetAuthSchId(this ClaimsPrincipal claimsPrincipal)
    {
        string? value = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (value != null)
            return Guid.Parse(value);
        return null;
    }
}
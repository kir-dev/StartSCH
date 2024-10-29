using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace StartSch.Auth;

// Based on
// https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-8.0&pivots=without-bff-pattern
public static class AuthSchExtensions
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
                oidcOptions.Scope.Remove("profile");

                // To retrieve a claim only available through the AuthSCH user info endpoint,
                // enable the following option and add a mapping:
                // oidcOptions.GetClaimsFromUserInfoEndpoint = true;
                // oidcOptions.Scope.Add("pek.sch.bme.hu:profile");
                // oidcOptions.ClaimActions.Add(new CustomJsonClaimAction("address", ClaimValueTypes.String,
                //     json => json.GetProperty("address").GetProperty("formatted").GetString()));

                // AuthSCH doesn't support single sign-out so all AuthSCH clients seem to just clear all cookies without
                // also logging the user out of AuthSCH.
                // The OpenIdConnectHandler would redirect the user to the AuthSCH sign out page then would request the
                // user to be redirected back using the post_logout_redirect_uri query parameter which is not supported
                // by AuthSCH meaning the user is left on the AuthSCH sign in screen.
                // We disable this by redirecting sign out requests to the Cookies auth handler thereby shortcutting
                // OIDC handler.
                // https://openid.net/specs/openid-connect-rpinitiated-1_0.html#RPLogout
                oidcOptions.ForwardSignOut = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
        services.AddAuthorization();
        services.AddCascadingAuthenticationState();

        services.AddSingleton<CookieOidcRefresher>();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<CookieOidcRefresher>((cookieOptions, refresher) =>
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
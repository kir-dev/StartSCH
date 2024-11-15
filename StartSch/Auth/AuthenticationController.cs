using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace StartSch.Auth;

// Based on https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-8.0&pivots=without-bff-pattern
[ApiController]
public class AuthenticationController : ControllerBase
{
    [HttpGet("/authentication/login")]
    public ChallengeResult Login([FromQuery] string? returnUrl)
    {
        return Challenge(new AuthenticationProperties()
        {
            RedirectUri = GetReturnUrl(returnUrl, Request.PathBase),
            IsPersistent = true
        });
    }

    [HttpPost("/authentication/logout")]
    public SignOutResult Logout([FromForm] string? returnUrl)
    {
        return SignOut(
            new AuthenticationProperties()
            {
                RedirectUri = GetReturnUrl(returnUrl, Request.PathBase)
            },
            CookieAuthenticationDefaults.AuthenticationScheme

            // AuthSCH doesn't support single sign-out, so other AuthSCH clients seem to just clear all cookies without
            // also logging the user out of AuthSCH.
            //
            // To do this, we don't sign out of the AuthSCH OIDC handler (Constants.AuthSchAuthenticationScheme),
            // otherwise the user would be redirected to the AuthSCH sign out page, and as AuthSCH doesn't support
            // post_logout_redirect_uri, get stuck there.
            // https://openid.net/specs/openid-connect-rpinitiated-1_0.html#RPLogout
        );
    }

    private static string? GetReturnUrl(string? returnUrl, string pathBase)
    {
        // Prevent open redirects
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = $"/{pathBase}";
        }
        else if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            returnUrl = new Uri(returnUrl, UriKind.Absolute).PathAndQuery;
        }
        else if (returnUrl[0] != '/')
        {
            returnUrl = $"/{pathBase}/{returnUrl}";
        }

        return returnUrl;
    }
}
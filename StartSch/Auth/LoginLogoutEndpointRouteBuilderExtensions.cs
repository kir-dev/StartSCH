using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace StartSch.Auth;

internal static class LoginLogoutEndpointRouteBuilderExtensions
{
    internal static IEndpointConventionBuilder MapLoginAndLogout(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("");

        group.MapGet("/login", (string? returnUrl, HttpContext httpContext)
                => TypedResults.Challenge(GetAuthProperties(returnUrl, httpContext.Request.PathBase)))
            .AllowAnonymous();

        // Sign out of the Cookie and OIDC handlers. If you do not sign out with the OIDC handler,
        // the user will automatically be signed back in the next time they visit a page that requires authentication
        // without being able to choose another account.
        group.MapPost(
            "/logout", ([FromForm] string? returnUrl, HttpContext httpContext)
                => TypedResults.SignOut(
                    GetAuthProperties(returnUrl, httpContext.Request.PathBase),
                    [
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        Constants.AuthSchAuthenticationScheme
                    ]
                )
        );

        return group;
    }

    private static AuthenticationProperties GetAuthProperties(string? returnUrl, string pathBase)
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

        return new() { RedirectUri = returnUrl };
    }
}
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using StartSch.Authorization.Requirements;
using StartSch.Services;

namespace StartSch;

// Blazor OIDC sample: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-8.0&pivots=without-bff-pattern
// Issue: no OAuth2 refresh token support in ASP.NET https://github.com/dotnet/aspnetcore/issues/8175
// OIDC token refresh library: https://docs.duendesoftware.com/foss/accesstokenmanagement/web_apps/
public static class AuthSchSetup
{
}

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using StartSch;
using StartSch.Components;
using StartSch.Modules.Cmsch;
using StartSch.Modules.GeneralEvent;
using StartSch.Modules.SchPincer;

const string MsOidcScheme = "MicrosoftOidc";

var builder = WebApplication.CreateBuilder(args);

string oidcClientSecret = builder.Configuration["Oidc:ClientSecret"]
                          ?? throw new InvalidOperationException("Oidc:ClientSecret not set");

// https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-8.0&pivots=without-bff-pattern
builder.Services.AddAuthentication(MsOidcScheme)
    .AddOpenIdConnect(MsOidcScheme, oidcOptions =>
    {
        // The OIDC handler must use a sign-in scheme capable of persisting
        // user credentials across requests.
        oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

        // The "openid" and "profile" scopes are required for the OIDC handler
        // and included by default. You should enable these scopes here if scopes
        // are provided by "Authentication:Schemes:MicrosoftOidc:Scope"
        // configuration because configuration may overwrite the scopes collection.
        //oidcOptions.Scope.Add(OpenIdConnectScope.OpenIdProfile);

        // The following paths must match the redirect and post logout redirect
        // paths configured when registering the application with the OIDC provider.
        // For Microsoft Entra ID, this is accomplished through the "Authentication"
        // blade of the application's registration in the Azure portal. Both the
        // signin and signout paths must be registered as Redirect URIs. The default
        // values are "/signin-oidc" and "/signout-callback-oidc".
        // Microsoft Identity currently only redirects back to the
        // SignedOutCallbackPath if authority is
        // https://login.microsoftonline.com/{TENANT ID}/v2.0/ as it is above.
        // You can use the "common" authority instead, and logout redirects back to
        // the Blazor app. For more information, see
        // https://github.com/AzureAD/microsoft-authentication-library-for-js/issues/5783
        //oidcOptions.CallbackPath = new PathString("/signin-oidc");
        //oidcOptions.SignedOutCallbackPath = new PathString("/signout-callback-oidc");

        // The RemoteSignOutPath is the "Front-channel logout URL" for remote single
        // sign-out. The default value is "/signout-oidc".
        //oidcOptions.RemoteSignOutPath = new PathString("/signout-oidc");

        // The following example Authority is configured for Microsoft Entra ID
        // and a single-tenant application registration. Set the {TENANT ID}
        // placeholder to the Tenant ID. The "common" Authority
        // https://login.microsoftonline.com/common/v2.0/ should be used
        // for multi-tenant apps. You can also use the "common" Authority for
        // single-tenant apps, but it requires a custom IssuerValidator as shown
        // in the comments below.
        oidcOptions.Authority = "https://auth.sch.bme.hu/";

        // Set the Client ID for the app. Set the {CLIENT ID} placeholder to
        // the Client ID.
        oidcOptions.ClientId = "76973065421620761369";

        // Setting ResponseType to "code" configures the OIDC handler to use
        // authorization code flow. Implicit grants and hybrid flows are unnecessary
        // in this mode. In a Microsoft Entra ID app registration, you don't need to
        // select either box for the authorization endpoint to return access tokens
        // or ID tokens. The OIDC handler automatically requests the appropriate
        // tokens using the code returned from the authorization endpoint.
        oidcOptions.ResponseType = OpenIdConnectResponseType.Code;

        // Set MapInboundClaims to "false" to obtain the original claim types from
        // the token. Many OIDC servers use "name" and "role"/"roles" rather than
        // the SOAP/WS-Fed defaults in ClaimTypes. Adjust these values if your
        // identity provider uses different claim types.
        oidcOptions.MapInboundClaims = false;
        oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
        oidcOptions.TokenValidationParameters.RoleClaimType = "roles";

        // Many OIDC providers work with the default issuer validator, but the
        // configuration must account for the issuer parameterized with "{TENANT ID}"
        // returned by the "common" endpoint's /.well-known/openid-configuration
        // For more information, see
        // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/1731
        //var microsoftIssuerValidator = AadIssuerValidator.GetAadIssuerValidator(oidcOptions.Authority);
        //oidcOptions.TokenValidationParameters.IssuerValidator = microsoftIssuerValidator.Validate;

        // OIDC connect options set later via ConfigureCookieOidcRefresh
        //
        // (1) The "offline_access" scope is required for the refresh token.
        //
        // (2) SaveTokens is set to true, which saves the access and refresh tokens
        // in the cookie, so the app can authenticate requests for weather data and
        // use the refresh token to obtain a new access token on access token
        // expiration.
        oidcOptions.ClientSecret = oidcClientSecret;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

// ConfigureCookieOidcRefresh attaches a cookie OnValidatePrincipal callback to get
// a new access token when the current one expires, and reissue a cookie with the
// new access token saved inside. If the refresh fails, the user will be signed
// out. OIDC connect options are set for saving tokens and the offline access
// scope.
builder.Services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, MsOidcScheme);

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddModule<CmschModule>();
builder.Services.AddModule<GeneralEventModule>();
builder.Services.AddModule<SchPincerModule>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StartSch.Wasm.Components._Imports).Assembly);

app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();
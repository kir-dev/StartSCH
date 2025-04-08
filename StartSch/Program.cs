using System.Net;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using StartSch;
using StartSch.Authorization.Handlers;
using StartSch.Authorization.Requirements;
using StartSch.Components;
using StartSch.Data;
using StartSch.Modules.Cmsch;
using StartSch.Modules.GeneralEvent;
using StartSch.Modules.SchBody;
using StartSch.Modules.SchPincer;
using StartSch.Services;

var builder = WebApplication.CreateBuilder(args);

// Add custom options
builder.Services.Configure<StartSchOptions>(builder.Configuration.GetSection("StartSch"));

// Services
builder.Services.AddSingletonAndHostedService<NotificationQueueService>();
builder.Services.AddHostedService<PollJobService>();
builder.Services.AddSingleton<BlazorTemplateRenderer>();
builder.Services.AddSingleton<TagService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<UserInfoService>();

// Modules
builder.Services.AddModule<CmschModule>();
builder.Services.AddModule<GeneralEventModule>();
builder.Services.AddModule<SchBodyModule>();
builder.Services.AddModule<SchPincerModule>();

// Authentication
builder.Services.AddAuthentication(options =>
    {
        // Sign in using AuthSCH
        options.DefaultChallengeScheme = Constants.AuthSchAuthenticationScheme;

        // Store the user's identity in a cookie
        options.DefaultScheme = Constants.CookieAuthenticationScheme;
    })
    .AddCookie(Constants.CookieAuthenticationScheme, options => options.Cookie.Name = "User")
    .AddOpenIdConnect(Constants.AuthSchAuthenticationScheme, options =>
    {
        options.Authority = "https://auth.sch.bme.hu";

        options.ClientId = builder.Configuration["AuthSch:ClientId"];
        options.ClientSecret = builder.Configuration["AuthSch:ClientSecret"];

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("offline_access");
        options.Scope.Add("pek.sch.bme.hu:profile");
        options.Scope.Add("email");
        // To retrieve a claim only available through the AuthSCH user info endpoint
        // (https://git.sch.bme.hu/kszk/authsch/-/wikis/api#a-userinfo-endpoint),
        // add its corresponding scope here, then map the claim in UserInfoService.

        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = "roles";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
    });
builder.Services.AddCascadingAuthenticationState();

// After the user logs in, we receive an Authorization Code from AuthSCH, which is then automatically redeemed
// by ASP.NET for an access token, an ID token and a refresh token.
// These are then stored in the user's cookie (`options.SaveTokens = true`).
//
// As the ID token does not contain things like group memberships, the AuthSCH UserInfo endpoint is queried using the
// access token (`options.GetClaimsFromUserInfoEndpoint = true`).
// The UserInfo endpoint returns JSON data, as documented on the AuthSCH wiki.
//
// In the below code, we hook into the UserInformationReceived event to set the "memberships" claim for the user,
// and as the UserInfo endpoint returns the IDs and names of the groups the user is a member of, we add these groups
// to the database.
string publicUrl = builder.Configuration["StartSch:PublicUrl"]!;
builder.Services.AddOptions<OpenIdConnectOptions>(Constants.AuthSchAuthenticationScheme)
    .PostConfigure(((OpenIdConnectOptions options, IServiceProvider serviceProvider) =>
    {
        options.Events.OnUserInformationReceived = async context =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await scope.ServiceProvider
                .GetRequiredService<UserInfoService>()
                .OnUserInformationReceived(context);
        };

        options.Backchannel.DefaultRequestHeaders.Add("User-Agent", $"StartSCH/1.0 (+{publicUrl})");
    }));

// Authorization
builder.Services.AddSingleton<IAuthorizationHandler, EventReadAccessHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PublishedPostAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EventAdminAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PostAdminAccessHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.AddRequirements(AdminRequirement.Instance))
    .AddPolicy("GroupAdmin", policy => policy.AddRequirements(GroupAdminRequirement.Instance))
    .AddPolicy("Write", policy => policy.AddRequirements(ResourceAccessRequirement.Write));

// Database
//    Register SqliteDb
builder.Services.AddPooledDbContextFactory<SqliteDb>(db =>
{
    db.UseSqlite(builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=StartSch.db");
    if (builder.Environment.IsDevelopment()) db.EnableSensitiveDataLogging();
});

//    Register PostgresDb if there is a connection string
string? postgresConnectionString = builder.Configuration.GetConnectionString("Postgres");
if (postgresConnectionString != null)
{
    builder.Services.AddPooledDbContextFactory<PostgresDb>(db =>
    {
        db.UseNpgsql(postgresConnectionString);
        if (builder.Environment.IsDevelopment()) db.EnableSensitiveDataLogging();
    });
}

//    Register Db and IDbContextFactory<Db> using one of them
if (postgresConnectionString != null)
{
    builder.Services.AddSingleton<IDbContextFactory<Db>>(sp =>
        new DbContextFactoryTranslator<PostgresDb>(sp.GetRequiredService<IDbContextFactory<PostgresDb>>()));
    builder.Services.AddScoped<Db>(sp => sp.GetRequiredService<IDbContextFactory<PostgresDb>>().CreateDbContext());
}
else
{
    builder.Services.AddSingleton<IDbContextFactory<Db>>(sp =>
        new DbContextFactoryTranslator<SqliteDb>(sp.GetRequiredService<IDbContextFactory<SqliteDb>>()));
    builder.Services.AddScoped<Db>(sp => sp.GetRequiredService<IDbContextFactory<SqliteDb>>().CreateDbContext());
}

builder.Services.AddDataProtection().PersistKeysToDbContext<Db>();

// Email service
string? kirMailApiKey = builder.Configuration["KirMail:ApiKey"];
if (kirMailApiKey != null)
{
    builder.Services.AddHttpClient(nameof(KirMailService), client =>
    {
        client.DefaultRequestHeaders.Authorization = new("Api-Key", kirMailApiKey);
        client.DefaultRequestHeaders.Add("User-Agent", $"StartSCH/1 (+{publicUrl})");
    });
    builder.Services.AddSingleton<IEmailService, KirMailService>();
}
else
    builder.Services.AddSingleton<IEmailService, NoopEmailService>();

// Push notifications
// https://tpeczek.github.io/Lib.Net.Http.WebPush/articles/aspnetcore-integration.html
builder.Services.AddMemoryCache();
builder.Services.AddMemoryVapidTokenCache();
builder.Services.AddPushServiceClient(builder.Configuration.GetSection("Push").Bind);

// Set original IP address and protocol from headers set by the reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear(); // trust headers from all proxies
    options.KnownNetworks.Clear();
});

// Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

// API controllers
builder.Services.AddControllersWithViews(); // WithViews is needed to use the ValidateAntiForgeryToken attribute

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

{
    await using var serviceScope = app.Services.CreateAsyncScope();
    await serviceScope.ServiceProvider.GetRequiredService<Db>().Database.MigrateAsync();
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware#middleware-order
app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StartSch.Wasm._Imports).Assembly);

app.Run();

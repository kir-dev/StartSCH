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

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(); // WithViews is needed for antiforgery
builder.Services.AddDataProtection().PersistKeysToDbContext<Db>();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});
builder.Services.Configure<StartSchOptions>(builder.Configuration.GetSection("StartSch"));

// Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("cookie", options => { options.Cookie.Name = "web"; })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = "https://auth.sch.bme.hu";

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
builder.Services.AddCascadingAuthenticationState();

// After the user logs in, we receive an Authorization Code from AuthSCH, which is then automatically redeemed
// by ASP.NET for an access token, an ID token and a refresh token.
// These are then stored in the user's cookie (`options.SaveTokens = true`).
//
// As the ID token does not contain things like group memberships, the AuthSCH UserInfo endpoint is queried using the
// access token (`options.GetClaimsFromUserInfoEndpoint = true`).
// The UserInfo endpoint returns JSON data, documented on the AuthSCH wiki.
//
// In the below code, we hook into the UserInformationReceived event to set the "memberships" claim for the user,
// and as the UserInfo endpoint returns the IDs and names of the groups the user is a member of, we add these groups
// to the database.
builder.Services.AddOptions<OpenIdConnectOptions>("oidc")
    .Configure(((OpenIdConnectOptions options, IServiceProvider serviceProvider) =>
    {
        options.Events.OnUserInformationReceived = async context =>
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            await scope.ServiceProvider
                .GetRequiredService<UserInfoService>()
                .OnUserInformationReceived(context);
        };
    }));

// Authorization
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.AddRequirements(AdminRequirement.Instance))
    .AddPolicy("GroupAdmin", policy => policy.AddRequirements(GroupAdminRequirement.Instance))
    .AddPolicy("Write", policy => policy.AddRequirements(ResourceAccessRequirement.Write));

// Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

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

// Email service
string? kirMailApiKey = builder.Configuration["KirMailApiKey"];
if (kirMailApiKey != null)
{
    builder.Services.AddHttpClient(nameof(KirMailService),
        client =>
        {
            client.DefaultRequestHeaders.Authorization = new("Api-Key", kirMailApiKey);
            client.DefaultRequestHeaders.Add("User-Agent", "StartSCH/1 (https://start.alb1.hu)");
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

// Modules
builder.Services.AddModule<CmschModule>();
builder.Services.AddModule<GeneralEventModule>();
builder.Services.AddModule<SchBodyModule>();
builder.Services.AddModule<SchPincerModule>();

// Services
builder.Services.AddSingletonAndHostedService<NotificationQueueService>();
builder.Services.AddHostedService<PollJobService>();
builder.Services.AddSingleton<BlazorTemplateRenderer>();
builder.Services.AddSingleton<TagService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<UserInfoService>();

// Module-agnostic authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, EventReadAccessHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PublishedPostAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EventAdminAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PostAdminAccessHandler>();

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

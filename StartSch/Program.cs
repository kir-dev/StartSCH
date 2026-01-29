using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using NodaTime.Serialization.SystemTextJson;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StartSch;
using StartSch.Authorization.Handlers;
using StartSch.Authorization.Requirements;
using StartSch.BackgroundTasks;
using StartSch.BackgroundTasks.Handlers;
using StartSch.Components;
using StartSch.Data;
using StartSch.Data.Migrations;
using StartSch.Modules.Cmsch;
using StartSch.Modules.GeneralEvent;
using StartSch.Modules.KthBmeHu;
using StartSch.Modules.SchBody;
using StartSch.Modules.SchPincer;
using StartSch.Modules.VikBmeHu;
using StartSch.Modules.VikHk;
using StartSch.Services;

var builder = WebApplication.CreateBuilder(args);

// Modules
builder.Services.AddModule<CmschModule>();
builder.Services.AddModule<GeneralEventModule>();
builder.Services.AddModule<KthBmeHuModule>();
builder.Services.AddModule<SchBodyModule>();
builder.Services.AddModule<SchPincerModule>();
builder.Services.AddModule<VikBmeHuModule>();
builder.Services.AddModule<VikHkModule>();

// Services
builder.Services.AddSingletonAndHostedService<BackgroundTaskManager>();
builder.Services.AddHostedService<PollJobService>();
builder.Services.AddSingleton<BlazorTemplateRenderer>();
builder.Services.AddSingleton<FontCache>();
builder.Services.AddSingleton<PushSubscriptionService>();
builder.Services.AddScoped<InterestService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<UserInfoService>();
builder.Services.AddTransient<ModuleInitializationService>();

// Background task handlers
builder.Services
    .AddScopedBackgroundTaskHandler<SendEmail, SendEmailHandler>(3, 100)
    .AddScopedBackgroundTaskHandler<SendPushNotification, SendPushNotificationHandler>(20)
    .AddScopedBackgroundTaskHandler<CreateOrderingStartedNotifications, CreateOrderingStartedNotificationsHandler>(
        1, 1, true)
    .AddScopedBackgroundTaskHandler<CreatePostPublishedNotifications, CreatePostPublishedNotificationsHandler>(
        1, 1, true)
    ;

// Custom options
builder.Services.Configure<StartSchOptions>(builder.Configuration.GetSection("StartSch"));

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
        options.MapInboundClaims = false; // Disable messing with claim names
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
// In the below code, we hook into the UserInformationReceived event to set the "memberships" claim for the user.
// As the UserInfo endpoint returns the IDs and names of the groups the user is a member of, we add these groups
// to the database.
string publicUrl = builder.Configuration["StartSch:PublicUrl"]!;
string userAgent = $"StartSCHBot/1.0 (+{publicUrl})";
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

        options.Backchannel.DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent);
    }));

// Authorization
builder.Services.AddSingleton<IAuthorizationHandler, EventReadAccessHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PublishedPostAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EventAdminAccessHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PostAdminAccessHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.AddRequirements(AdminRequirement.Instance))
    .AddPolicy(PageAdminRequirement.Policy, policy => policy.AddRequirements(PageAdminRequirement.Instance))
    .AddPolicy("Write", policy => policy.AddRequirements(ResourceAccessRequirement.Write));

// Database
//    Register SqliteDb
builder.Services.AddPooledDbContextFactory<SqliteDb>(db =>
{
    db.UseSqlite(
        builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=StartSch.db",
        o => o.UseNodaTime()
    );
    
    if (builder.Environment.IsDevelopment())
        db.EnableSensitiveDataLogging();
});

//    Register PostgresDb if there is a connection string
string? postgresConnectionString = builder.Configuration.GetConnectionString("Postgres");
if (postgresConnectionString != null)
{
    builder.Services.AddPooledDbContextFactory<PostgresDb>(db =>
    {
        db.UseNpgsql(
            postgresConnectionString,
            o => o.UseNodaTime()
        );
        
        if (builder.Environment.IsDevelopment())
            db.EnableSensitiveDataLogging();
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

// HTTP clients
builder.Services
    .ConfigureHttpClientDefaults(httpClientBuilder => httpClientBuilder
        .ConfigureHttpClient(httpClient => httpClient
            .DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent))
        .UseSocketsHttpHandler((handler, _) => handler
            .ConnectCallback = HappyEyeballs.SocketsHttpHandlerConnectCallback));
builder.Services.AddHttpClient<WordPressHttpClient>();

// Email service
string? kirMailApiKey = builder.Configuration["KirMail:ApiKey"];
if (kirMailApiKey != null)
{
    builder.Services.AddHttpClient(
        nameof(KirMailService),
        client => client.DefaultRequestHeaders.Authorization = new("Api-Key", kirMailApiKey)
    );
    builder.Services.AddSingleton<IEmailService, KirMailService>();
}
else
    builder.Services.AddSingleton<IEmailService, NoopEmailService>();

// Push notifications
// https://tpeczek.github.io/Lib.Net.Http.WebPush/articles/aspnetcore-integration.html
builder.Services.AddMemoryCache();
builder.Services.AddMemoryVapidTokenCache();
builder.Services.AddPushServiceClient(builder.Configuration.GetSection("Push").Bind);

// Set the requester's IP address and the original protocol using headers set by the reverse proxy
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
builder.Services
    .AddControllersWithViews() // WithViews is needed to use the ValidateAntiForgeryToken attribute
    .AddJsonOptions(o => o.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb)); 

builder.Services.AddHttpContextAccessor();

// OpenTelemetry: logs, metrics and traces.
//     Automatically sends everything to the endpoint specified by the OTEL_EXPORTER_OTLP_ENDPOINT env var
builder.Services
    .AddOpenTelemetry()
    .UseOtlpExporter()
    .ConfigureResource(resource => resource.AddService("startsch"))
    .WithLogging()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
    )
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("*")
    );

var app = builder.Build();

{
    await using var serviceScope = app.Services.CreateAsyncScope();
    var services = serviceScope.ServiceProvider;
    await services.GetRequiredService<Db>().Database.MigrateAsync();

    await app.Services.GetRequiredService<ModuleInitializationService>().InitializeModules();
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

app.Use((context, next) =>
{
    context.Response.OnStarting(() =>
    {
        // When a Blazor page is rendered on the server, IAntiforgery.GetAndStoreTokens is called, which forces
        // Cache-Control to be "no-cache, no-store", thereby disabling the bfcache. Override it.
        // https://web.dev/articles/bfcache
        // https://github.com/dotnet/aspnetcore/issues/54464
        if (context.Response.Headers.TryGetValue(HeaderNames.CacheControl, out var cacheControlHeader)
            && cacheControlHeader == "no-cache, no-store")
            context.Response.Headers.CacheControl = "no-cache";
        return Task.CompletedTask;
    });

    return next();
});

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StartSch.Wasm._Imports).Assembly);

if (app.Services.GetRequiredService<IOptions<StartSchOptions>>().Value.DisallowBots)
{
    app.Map("/robots.txt", () =>
        """
        User-agent: *
        Disallow: /
        """
    );
}

await app.RunAsync();

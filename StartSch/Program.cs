using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using StartSch;
using StartSch.Authorization.Handlers;
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

// Authentication and authorization
builder.Services.AddAuthSch();

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

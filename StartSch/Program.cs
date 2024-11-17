using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using StartSch;
using StartSch.Auth;
using StartSch.Components;
using StartSch.Data;
using StartSch.Modules.Cmsch;
using StartSch.Modules.GeneralEvent;
using StartSch.Modules.SchPincer;
using StartSch.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

// Authentication and authorization
builder.Services.AddAuthSch();

// Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();

// Database
string? postgresConnectionString = builder.Configuration.GetConnectionString("Db");
builder.Services.AddPooledDbContextFactory<Db>(dbOptions =>
{
    DbContextOptionsBuilder dbBuilder = postgresConnectionString == null
        ? dbOptions.UseSqlite("Data Source=StartSch.db")
        : dbOptions.UseNpgsql(postgresConnectionString);
    if (builder.Environment.IsDevelopment())
        dbBuilder.EnableSensitiveDataLogging();
});
if (postgresConnectionString != null) // only used by migrations
    builder.Services.AddDbContext<PostgresDb>(o => o.UseNpgsql(postgresConnectionString));
builder.Services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<Db>>().CreateDbContext());

// Email service
string? kirMailApiKey = builder.Configuration["KirMailApiKey"];
if (kirMailApiKey != null)
{
    builder.Services.AddHttpClient(nameof(KirMailService),
        client => client.DefaultRequestHeaders.Authorization = new("Api-Key", kirMailApiKey));
    builder.Services.AddSingleton<IEmailService, KirMailService>();
}
else
    builder.Services.AddSingleton<IEmailService, DummyEmailService>();

// Push notifications
// https://tpeczek.github.io/Lib.Net.Http.WebPush/articles/aspnetcore-integration.html
builder.Services.AddMemoryCache();
builder.Services.AddMemoryVapidTokenCache();
builder.Services.AddPushServiceClient(builder.Configuration.GetSection("Push").Bind);
builder.Services.AddScoped<PushService>();

// Modules
builder.Services.AddModule<CmschModule>();
builder.Services.AddModule<GeneralEventModule>();
builder.Services.AddModule<SchPincerModule>();
builder.Services.AddHostedService<CronService>();

var app = builder.Build();

{
    await using var serviceScope = app.Services.CreateAsyncScope();
    Db db = postgresConnectionString == null
        ? serviceScope.ServiceProvider.GetRequiredService<Db>()
        : serviceScope.ServiceProvider.GetRequiredService<PostgresDb>();
    await db.Database.MigrateAsync();
}

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

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StartSch.Wasm._Imports).Assembly);

app.Run();
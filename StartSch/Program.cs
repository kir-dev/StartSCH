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

// Authentication and authorization
builder.Services.AddAuthSch();

// Blazor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();

// Database
string? postgresConnectionString = builder.Configuration.GetConnectionString("Db");
if (builder.Environment.IsDevelopment() || postgresConnectionString == null)
    builder.Services.AddPooledDbContextFactory<Db>(dbOptions => dbOptions
        .UseSqlite("Data Source=StartSch.db")
        .EnableSensitiveDataLogging());
else
    builder.Services.AddPooledDbContextFactory<Db>(dbOptions => dbOptions
        .UseNpgsql(postgresConnectionString));
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

// Modules
builder.Services.AddModule<CmschModule>();
builder.Services.AddModule<GeneralEventModule>();
builder.Services.AddModule<SchPincerModule>();
builder.Services.AddHostedService<CronService>();

var app = builder.Build();

{
    await using var serviceScope = app.Services.CreateAsyncScope();
    await serviceScope
        .ServiceProvider.GetRequiredService<Db>()
        .Database.MigrateAsync();
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(StartSch.Wasm._Imports).Assembly);

app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();
using Microsoft.AspNetCore.Components.Authorization;
using StartSch;
using StartSch.Auth;
using StartSch.Components;
using StartSch.Modules.Cmsch;
using StartSch.Modules.GeneralEvent;
using StartSch.Modules.SchPincer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthSch();

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
    .AddAdditionalAssemblies(typeof(StartSch.Wasm._Imports).Assembly);

app.MapGroup("/authentication").MapLoginAndLogout();

app.Run();
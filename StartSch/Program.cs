using StartSch;
using StartSch.Components;
using StartSch.Modules.Cmsch;
using StartSch.Modules.GeneralEvent;
using StartSch.Modules.SchPincer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddModule<CmschModule>();
builder.Services.AddModule<GeneralEventModule>();
builder.Services.AddModule<SchPincerModule>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
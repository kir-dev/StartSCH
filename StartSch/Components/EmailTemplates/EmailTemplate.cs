using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace StartSch.Components.EmailTemplates;

public class EmailTemplate : ComponentBase
{
    [Inject] public required IOptions<StartSchOptions> StartSchOptions { get; set; }

    [field: AllowNull, MaybeNull]
    protected string BaseUrl => field ??= StartSchOptions.Value.PublicUrl;

    [field: AllowNull, MaybeNull]
    protected string PreferencesUrl => field ??= BaseUrl + "/preferences";
}

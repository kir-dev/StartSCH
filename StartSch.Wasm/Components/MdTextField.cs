using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace StartSch.Wasm.Components;

/// Wrapper around md-filled-text-field that fixes updates to the value
/// from Blazor not being reflected.
/// 
/// Usage:
///     @bind-Value="@myValue"
/// 
/// https://stackoverflow.com/questions/77583025/how-to-use-bind-not-bind-property-or-bind-on-dom-property-for-custom-element
public class MdTextField : ComponentBase
{
    protected ElementReference _elementRef;
    private string? _lastValue;

    [Inject] private IJSRuntime Js { get; set; } = null!;

    [Parameter] public string? Value { get; set; }
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    [Parameter] public EventCallback<ChangeEventArgs> OnChange { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Value != _lastValue)
        {
            await Js.InvokeVoidAsync("setElementProperty", _elementRef, "value", Value ?? "");
            _lastValue = Value;
        }
    }

    protected async Task OnChangeHandler(ChangeEventArgs e)
    {
        var newValue = (string?)e.Value;
        _lastValue = newValue;
        await ValueChanged.InvokeAsync(newValue);
        await OnChange.InvokeAsync(e);
    }
}

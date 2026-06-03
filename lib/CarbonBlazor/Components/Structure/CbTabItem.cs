using Microsoft.AspNetCore.Components;

namespace CarbonBlazor.Components.Structure;

public sealed class CbTabItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = "Tab";
    public bool Disabled { get; set; }
    public RenderFragment? Content { get; set; }
}

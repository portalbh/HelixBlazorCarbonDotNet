using Microsoft.AspNetCore.Components;

namespace CarbonBlazor.Components.Data;

public sealed class CbDataTableColumn<TItem>
{
    public string Header { get; set; } = string.Empty;
    public Func<TItem, object?>? SortKey { get; set; }
    public Func<TItem, string?>? Text { get; set; }
    public RenderFragment<TItem>? Template { get; set; }
    public string? Width { get; set; }
}

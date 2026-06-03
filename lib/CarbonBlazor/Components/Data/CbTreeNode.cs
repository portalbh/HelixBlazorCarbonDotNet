namespace CarbonBlazor.Components.Data;

public sealed class CbTreeNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Label { get; set; } = "Node";
    public bool Expanded { get; set; }
    public bool Disabled { get; set; }
    public List<CbTreeNode> Children { get; set; } = [];
}

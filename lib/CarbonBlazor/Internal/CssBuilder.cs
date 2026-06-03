namespace CarbonBlazor.Internal;

internal sealed class CssBuilder
{
    private readonly List<string> _classes = [];

    public CssBuilder(string? value = null)
    {
        Add(value);
    }

    public CssBuilder Add(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _classes.Add(value);
        }

        return this;
    }

    public CssBuilder Add(string value, bool when)
    {
        if (when)
        {
            Add(value);
        }

        return this;
    }

    public override string ToString() => string.Join(" ", _classes);
}

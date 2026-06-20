namespace Kompass.OData.Url;

/// <summary>
/// Represents the resource path segments of an OData URL.
/// </summary>
public sealed class ResourcePath
{
    public IReadOnlyList<string> Segments { get; }

    public ResourcePath(IReadOnlyList<string> segments)
    {
        Segments = segments;
    }

    public override string ToString()
    {
        return string.Join("/", Segments);
    }
}

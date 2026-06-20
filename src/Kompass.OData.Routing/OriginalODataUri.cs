namespace Kompass.OData.Routing;

/// <summary>
/// Stores the original OData URI before path rewriting.
/// Retrieved via <see cref="ODataRoutingExtensions.GetOriginalODataUri"/>.
/// </summary>
public sealed class OriginalODataUri
{
    public Uri Uri { get; }

    public OriginalODataUri(Uri uri)
    {
        Uri = uri;
    }

    public override string ToString()
    {
        return Uri.ToString();
    }
}

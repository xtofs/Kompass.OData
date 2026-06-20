namespace Kompass.OData.Service;

using System.Text;

/// <summary>
/// Describes the shape of the payload being serialized, independent of the request path.
/// Used by <see cref="ContextUrlBuilder"/> to produce spec-conformant context URL fragments.
/// </summary>
public sealed class SerializationShape
{
    /// <summary>True when the payload is a single entity.</summary>
    public bool IsSingleEntity { get; init; }

    /// <summary>True when the payload is a collection.</summary>
    public bool IsCollection { get; init; }

    /// <summary>True when the payload is an entity reference ($ref).</summary>
    public bool IsReference { get; init; }

    /// <summary>True when the payload is a delta response ($delta).</summary>
    public bool IsDelta { get; init; }

    /// <summary>
    /// Property names included by $select. Null or empty means all properties (no projection).
    /// </summary>
    public IReadOnlyList<string>? SelectedProperties { get; init; }

    /// <summary>
    /// Fully-qualified derived type name when the payload uses a type cast
    /// (e.g. <c>Namespace.Book</c>). Null means no cast.
    /// </summary>
    public string? DerivedTypeName { get; init; }

    /// <summary>Collection payload — no projection, no flags.</summary>
    public static readonly SerializationShape Collection = new SerializationShape { IsCollection = true };

    /// <summary>Single-entity payload.</summary>
    public static readonly SerializationShape Entity = new SerializationShape { IsSingleEntity = true };

    /// <summary>Create a collection shape with $select projection.</summary>
    public static SerializationShape CollectionWithSelect(IReadOnlyList<string> properties)
    {
        return new SerializationShape { IsCollection = true, SelectedProperties = properties };
    }

    /// <summary>Create a single-entity shape with $select projection.</summary>
    public static SerializationShape EntityWithSelect(IReadOnlyList<string> properties)
    {
        return new SerializationShape { IsSingleEntity = true, SelectedProperties = properties };
    }
}

/// <summary>
/// Internal model for rendering a context URL fragment.
/// Built from a resource path and serialization shape, then rendered to the final string.
/// </summary>
internal sealed class ContextFragment
{
    public required string ResourcePath { get; set; }
    public bool IsEntity { get; set; }
    public bool IsReference { get; set; }
    public bool IsDelta { get; set; }
    public IReadOnlyList<string>? SelectedProperties { get; set; }
}

/// <summary>
/// Builds OData <c>@odata.context</c> URLs per OData 4.01 Protocol Chapter 10.
/// <para>
/// Context URLs describe the <em>type shape</em> of the response payload, not instance identity.
/// They are derived from the resolved EDM resource and the actual payload shape, not from
/// string manipulation of the request URI.
/// </para>
/// <para>
/// The context URL has the form: <c>{metadataDocumentUri}#{contextFragment}</c>.
/// This builder produces the full context URL given a metadata URI prefix and
/// resource-specific factory methods.
/// </para>
/// </summary>
internal sealed class ContextUrlBuilder
{
    private readonly string _metadataUri;

    /// <param name="metadataUri">
    /// The metadata document URI (e.g. <c>$metadata</c> for relative or
    /// <c>https://host/service/$metadata</c> for absolute).
    /// </param>
    internal ContextUrlBuilder(string metadataUri)
    {
        _metadataUri = metadataUri;
    }

    /// <summary>Default instance using a relative <c>$metadata</c> URI.</summary>
    internal static readonly ContextUrlBuilder Default = new ContextUrlBuilder("$metadata");

    // ── Resource-oriented factory methods ──────────────────────────────────

    /// <summary>
    /// Context URL for an entity set: <c>$metadata#EntitySet</c> or
    /// <c>$metadata#EntitySet/$entity</c>.
    /// </summary>
    internal string ForEntitySet(string entitySetName, SerializationShape shape)
    {
        return Build(entitySetName, shape);
    }

    /// <summary>
    /// Context URL for a singleton: <c>$metadata#SingletonName</c>.
    /// </summary>
    internal string ForSingleton(string singletonName, SerializationShape shape)
    {
        return Build(singletonName, shape);
    }

    /// <summary>
    /// Context URL for a contained navigation property:
    /// <c>$metadata#EntitySet/NavProp</c> or <c>$metadata#EntitySet/NavProp/$entity</c>.
    /// </summary>
    internal string ForContainedNavigation(string entitySetName, string navPropName, SerializationShape shape)
    {
        return Build($"{entitySetName}/{navPropName}", shape);
    }

    /// <summary>
    /// Context URL for a structural property value:
    /// <c>$metadata#EntitySet(key)/PropertyName</c>.
    /// </summary>
    internal string ForProperty(string entityPath, string propertyName)
    {
        return $"{_metadataUri}#{entityPath}/{propertyName}";
    }

    /// <summary>
    /// Context URL for an operation (function/action) result.
    /// The fragment is based on the return type's entity set, not the operation name.
    /// </summary>
    internal string ForOperationResult(string entitySetName, SerializationShape shape)
    {
        return Build(entitySetName, shape);
    }

    // ── Core build + render ────────────────────────────────────────────────

    private string Build(string resourcePath, SerializationShape shape)
    {
        var fragment = new ContextFragment { ResourcePath = resourcePath };

        // Derived type cast — append before projection
        if (shape.DerivedTypeName is not null)
        {
            fragment.ResourcePath += "/" + shape.DerivedTypeName;
        }

        // $select projection
        if (shape.SelectedProperties is { Count: > 0 })
        {
            fragment.SelectedProperties = shape.SelectedProperties;
        }

        // Payload suffixes
        fragment.IsReference = shape.IsReference;
        fragment.IsDelta = shape.IsDelta;
        fragment.IsEntity = shape.IsSingleEntity;

        return Render(fragment);
    }

    /// <summary>
    /// Renders a <see cref="ContextFragment"/> to the full context URL string.
    /// Suffix order per spec: select → $ref → $delta → $entity.
    /// </summary>
    private string Render(ContextFragment fragment)
    {
        var sb = new StringBuilder(_metadataUri);
        sb.Append('#');
        sb.Append(fragment.ResourcePath);

        if (fragment.SelectedProperties is { Count: > 0 })
        {
            sb.Append('(');
            sb.Append(string.Join(",", fragment.SelectedProperties));
            sb.Append(')');
        }

        if (fragment.IsReference)
        {
            sb.Append("/$ref");
        }

        if (fragment.IsDelta)
        {
            sb.Append("/$delta");
        }

        if (fragment.IsEntity)
        {
            sb.Append("/$entity");
        }

        return sb.ToString();
    }
}

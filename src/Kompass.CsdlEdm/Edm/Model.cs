namespace Kompass.CsdlEdm.Edm;

/// <summary>
/// A fully resolved CSDL document containing the version, references, and resolved schemas.
/// </summary>
public sealed class DocumentModel
{
    public required string Version { get; init; }
    public List<Reference> References { get; init; } = [];
    public List<Model> Schemas { get; init; } = [];
}

/// <summary>
/// A reference to an external document in the resolved model.
/// </summary>
public sealed class Reference
{
    public required string Uri { get; init; }
    public List<Include> Includes { get; init; } = [];
    public List<IncludeAnnotations> IncludeAnnotations { get; init; } = [];
}

/// <summary>
/// An Include within a resolved Reference.
/// </summary>
public sealed class Include
{
    public required string Namespace { get; init; }
    public string? Alias { get; init; }
}

/// <summary>
/// An IncludeAnnotations within a resolved Reference.
/// </summary>
public sealed class IncludeAnnotations
{
    public required string TermNamespace { get; init; }
    public string? TargetNamespace { get; init; }
    public string? Qualifier { get; init; }
}

/// <summary>
/// A single resolved schema (namespace) containing its elements and optional entity container.
/// </summary>
public sealed class Model
{
    public required string Namespace { get; init; }
    public string? Alias { get; init; }
    public List<SchemaElement> Elements { get; init; } = [];
    public EntityContainer? EntityContainer { get; init; }
}

/// <summary>
/// A resolved schema element.
/// </summary>
public abstract class SchemaElement
{
    public virtual string Name { get; init; } = "";
}

namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// Root CSDL document, wrapping an optional Edmx element.
/// This is the syntactic (unresolved) model produced by the XML/JSON readers.
/// </summary>
public sealed class CsdlDocument
{
    public Edmx? Edmx { get; set; }
}

/// <summary>
/// EDMX wrapper element with version, references, and data-services schemas.
/// </summary>
public sealed class Edmx
{
    public string? Version { get; set; }
    public List<Reference> References { get; set; } = [];
    public List<Schema> Schemas { get; set; } = [];
}

/// <summary>
/// A reference to an external CSDL document.
/// </summary>
public sealed class Reference
{
    public required string Uri { get; set; }
    public List<Include> Includes { get; set; } = [];
    public List<IncludeAnnotations> IncludeAnnotations { get; set; } = [];
}

/// <summary>
/// An Include directive within a Reference, importing a namespace with an optional alias.
/// </summary>
public sealed class Include
{
    public required string Namespace { get; set; }
    public string? Alias { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// An IncludeAnnotations directive within a Reference.
/// </summary>
public sealed class IncludeAnnotations
{
    public required string TermNamespace { get; set; }
    public string? Qualifier { get; set; }
    public string? TargetNamespace { get; set; }
}

/// <summary>
/// A CSDL schema with namespace, alias, and schema-level elements.
/// </summary>
public sealed class Schema
{
    public required string Namespace { get; set; }
    public string? Alias { get; set; }
    public List<SchemaElement> Elements { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

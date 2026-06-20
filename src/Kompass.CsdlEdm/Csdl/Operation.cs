namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// A CSDL Function declaration.
/// </summary>
public sealed class Function : SchemaElement
{
    public required override string Name { get; set; }
    public bool? IsBound { get; set; }
    public bool? IsComposable { get; set; }
    public string? EntitySetPath { get; set; }
    public List<Parameter> Parameters { get; set; } = [];
    public ReturnType? ReturnType { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL Action declaration.
/// </summary>
public sealed class Action : SchemaElement
{
    public required override string Name { get; set; }
    public bool? IsBound { get; set; }
    public string? EntitySetPath { get; set; }
    public List<Parameter> Parameters { get; set; } = [];
    public ReturnType? ReturnType { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A parameter on a Function or Action.
/// </summary>
public sealed class Parameter
{
    public required string Name { get; set; }
    public string? TypeName { get; set; }
    public bool IsCollection { get; set; }
    public bool? Nullable { get; set; }
    public MaxLengthFacet? MaxLength { get; set; }
    public string? Precision { get; set; }
    public ScaleFacet? Scale { get; set; }
    public SridFacet? Srid { get; set; }
    public bool? Unicode { get; set; }
    public string? DefaultValue { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// The return type of a Function or Action.
/// </summary>
public sealed class ReturnType
{
    public string? TypeName { get; set; }
    public bool IsCollection { get; set; }
    public bool? Nullable { get; set; }
    public MaxLengthFacet? MaxLength { get; set; }
    public string? Precision { get; set; }
    public ScaleFacet? Scale { get; set; }
    public SridFacet? Srid { get; set; }
    public bool? Unicode { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

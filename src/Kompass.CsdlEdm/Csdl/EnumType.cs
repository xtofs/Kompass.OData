namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// A CSDL EnumType declaration.
/// </summary>
public sealed class EnumType
{
    public required string Name { get; set; }
    public string? UnderlyingType { get; set; }
    public bool? IsFlags { get; set; }
    public List<EnumMember> Members { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A member of an EnumType.
/// </summary>
public sealed class EnumMember
{
    public required string Name { get; set; }
    public long? Value { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL TypeDefinition — a named alias for a primitive type with optional facets.
/// </summary>
public sealed class TypeDefinition
{
    public required string Name { get; set; }
    public required string UnderlyingType { get; set; }
    public MaxLengthFacet? MaxLength { get; set; }
    public string? Precision { get; set; }
    public ScaleFacet? Scale { get; set; }
    public SridFacet? Srid { get; set; }
    public bool? Unicode { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL Term declaration.
/// </summary>
public sealed class Term
{
    public required string Name { get; set; }
    public string? TypeName { get; set; }
    public bool IsCollection { get; set; }
    public string? BaseTerm { get; set; }
    public string? DefaultValue { get; set; }
    public List<string> AppliesTo { get; set; } = [];
    public bool? Nullable { get; set; }
    public MaxLengthFacet? MaxLength { get; set; }
    public string? Precision { get; set; }
    public ScaleFacet? Scale { get; set; }
    public SridFacet? Srid { get; set; }
    public bool? Unicode { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

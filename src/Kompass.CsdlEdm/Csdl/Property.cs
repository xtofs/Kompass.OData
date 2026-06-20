namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// A CSDL structural property (syntactic / unresolved).
/// </summary>
public sealed class Property
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
/// A CSDL navigation property (syntactic / unresolved).
/// </summary>
public sealed class NavigationProperty
{
    public required string Name { get; set; }
    public string? TypeName { get; set; }
    public bool IsCollection { get; set; }
    public bool? Nullable { get; set; }
    public string? Partner { get; set; }
    public bool? ContainsTarget { get; set; }
    public OnDeleteAction? OnDelete { get; set; }
    public List<ReferentialConstraint> ReferentialConstraints { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A referential constraint on a navigation property.
/// </summary>
public sealed class ReferentialConstraint
{
    public required string Property { get; set; }
    public required string ReferencedProperty { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// OnDelete action for a navigation property.
/// </summary>
public enum OnDeleteAction
{
    Cascade,
    None,
    SetNull,
    SetDefault,
}

public static class OnDeleteActionExtensions
{
    public static OnDeleteAction? Parse(string? raw)
    {
        return raw switch
        {
            "Cascade" => OnDeleteAction.Cascade,
            "None" => OnDeleteAction.None,
            "SetNull" => OnDeleteAction.SetNull,
            "SetDefault" => OnDeleteAction.SetDefault,
            _ => null,
        };
    }

    public static string ToEdmString(this OnDeleteAction action)
    {
        return action switch
        {
            OnDeleteAction.Cascade => "Cascade",
            OnDeleteAction.None => "None",
            OnDeleteAction.SetNull => "SetNull",
            OnDeleteAction.SetDefault => "SetDefault",
            _ => throw new ArgumentOutOfRangeException(nameof(action)),
        };
    }
}

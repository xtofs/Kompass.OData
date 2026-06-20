namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// A CSDL EntityType declaration (syntactic / unresolved).
/// </summary>
public sealed class EntityType : SchemaElement
{
    public required override string Name { get; set; }
    public string? BaseType { get; set; }
    public bool? Abstract { get; set; }
    public bool? OpenType { get; set; }
    public bool? HasStream { get; set; }
    public Key? Key { get; set; }
    public List<Property> Properties { get; set; } = [];
    public List<NavigationProperty> NavigationProperties { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL ComplexType declaration (syntactic / unresolved).
/// </summary>
public sealed class ComplexType : SchemaElement
{
    public required override string Name { get; set; }
    public string? BaseType { get; set; }
    public bool? Abstract { get; set; }
    public bool? OpenType { get; set; }
    public List<Property> Properties { get; set; } = [];
    public List<NavigationProperty> NavigationProperties { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL Key element containing one or more PropertyRef entries.
/// </summary>
public sealed class Key
{
    public List<PropertyRef> PropertyRefs { get; set; } = [];
}

/// <summary>
/// A PropertyRef within a Key, naming a key property.
/// </summary>
public sealed class PropertyRef
{
    public required string Name { get; set; }
}

namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// Base class for all schema-level element types (EntityType, ComplexType, EnumType, etc.).
/// </summary>
public abstract class SchemaElement
{
    public abstract string Name { get; set; }
}

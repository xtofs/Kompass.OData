namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// Base class for all schema-level element types (EntityType, ComplexType, EnumType, etc.).
/// Uses a sealed hierarchy to model the Rust enum <c>SchemaElement</c>.
/// </summary>
public abstract class SchemaElement
{
    public required string Name { get; set; }

    private SchemaElement() { }

    public sealed class EntityTypeElement : SchemaElement
    {
        public EntityType EntityType { get; set; } = null!;
    }

    public sealed class ComplexTypeElement : SchemaElement
    {
        public ComplexType ComplexType { get; set; } = null!;
    }

    public sealed class EnumTypeElement : SchemaElement
    {
        public EnumType EnumType { get; set; } = null!;
    }

    public sealed class TypeDefinitionElement : SchemaElement
    {
        public TypeDefinition TypeDefinition { get; set; } = null!;
    }

    public sealed class TermElement : SchemaElement
    {
        public Term Term { get; set; } = null!;
    }

    public sealed class FunctionElement : SchemaElement
    {
        public Function Function { get; set; } = null!;
    }

    public sealed class ActionElement : SchemaElement
    {
        public Action Action { get; set; } = null!;
    }

    public sealed class EntityContainerElement : SchemaElement
    {
        public EntityContainer EntityContainer { get; set; } = null!;
    }
}

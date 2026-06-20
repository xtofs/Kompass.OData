namespace Kompass.CsdlEdm.Edm;

/// <summary>
/// A resolved type reference: primitive, enum, complex, or type-definition.
/// Modeled as a sealed hierarchy.
/// </summary>
public abstract class ResolvedType
{
    private ResolvedType() { }

    public sealed class Primitive(PrimitiveType Type) : ResolvedType
    {
        public PrimitiveType Type { get; } = Type;
        public override string ToString() => $"Edm.{Type}";
    }

    public sealed class Enum(EnumType Type) : ResolvedType
    {
        public EnumType Type { get; } = Type;
        public override string ToString() => Type.Name;
    }

    public sealed class Complex(ComplexType Type) : ResolvedType
    {
        public ComplexType Type { get; } = Type;
        public override string ToString() => Type.Name;
    }

    public sealed class TypeDef(TypeDefinition Type) : ResolvedType
    {
        public TypeDefinition Type { get; } = Type;
        public override string ToString() => Type.Name;
    }
}

/// <summary>
/// A resolved type reference that can also be an entity type.
/// Used for Term types and operation parameter/return types.
/// </summary>
public abstract class TermType
{
    private TermType() { }

    public sealed class Primitive(PrimitiveType Type) : TermType
    {
        public PrimitiveType Type { get; } = Type;
    }

    public sealed class TypeDef(TypeDefinition Type) : TermType
    {
        public TypeDefinition Type { get; } = Type;
    }

    public sealed class Enum(EnumType Type) : TermType
    {
        public EnumType Type { get; } = Type;
    }

    public sealed class Complex(ComplexType Type) : TermType
    {
        public ComplexType Type { get; } = Type;
    }

    public sealed class Entity(EntityType Type) : TermType
    {
        public EntityType Type { get; } = Type;
    }
}

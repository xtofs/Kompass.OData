namespace Kompass.CsdlEdm.Edm;

/// <summary>
/// A resolved entity type with key properties, structural properties, and navigation properties.
/// In C# we use direct object references instead of Rust's Arc/Weak/OnceLock pattern.
/// </summary>
public sealed class EntityType
{
    public required string Name { get; init; }
    public bool IsAbstract { get; init; }
    public EntityType? BaseType { get; set; }

    /// <summary>Effective key paths (own + inherited).</summary>
    public List<IReadOnlyList<KeyPathSegment>> Keys { get; set; } = [];

    public List<Property> Properties { get; set; } = [];
    public List<NavigationProperty> NavigationProperties { get; set; } = [];
}

/// <summary>
/// A resolved complex type.
/// </summary>
public sealed class ComplexType
{
    public required string Name { get; init; }
    public bool IsAbstract { get; init; }
    public ComplexType? BaseType { get; set; }
    public List<Property> Properties { get; set; } = [];
    public List<NavigationProperty> NavigationProperties { get; set; } = [];
}

/// <summary>
/// A resolved enum type.
/// </summary>
public sealed class EnumType
{
    public required string Name { get; init; }
    public List<EnumMember> Members { get; init; } = [];
}

/// <summary>
/// A resolved enum member.
/// </summary>
public sealed class EnumMember
{
    public required string Name { get; init; }
    public long? Value { get; init; }
}

/// <summary>
/// A resolved type definition — a named alias for a primitive type.
/// </summary>
public sealed class TypeDefinition
{
    public required string Name { get; init; }
    public required PrimitiveType UnderlyingType { get; init; }
}

/// <summary>
/// A resolved structural property.
/// </summary>
public sealed class Property
{
    public required string Name { get; init; }
    public required ResolvedType Type { get; init; }
    public bool IsCollection { get; init; }
}

/// <summary>
/// A resolved navigation property. Uses direct object references (C# GC handles cycles).
/// </summary>
public sealed class NavigationProperty
{
    public required string Name { get; init; }
    public required EntityType Target { get; init; }
    public bool IsCollection { get; init; }
    public bool? ContainsTarget { get; init; }
    public Csdl.OnDeleteAction? OnDelete { get; init; }
    public List<ReferentialConstraint> ReferentialConstraints { get; init; } = [];

    /// <summary>Resolved partner path. Null when no partner is declared.</summary>
    public IReadOnlyList<BindingPathSegment>? Partner { get; set; }
}

/// <summary>
/// A resolved referential constraint.
/// </summary>
public sealed class ReferentialConstraint
{
    public required string Property { get; init; }
    public required string ReferencedProperty { get; init; }
}

/// <summary>
/// A resolved Term.
/// </summary>
public sealed class Term
{
    public required string Name { get; init; }
    public bool IsCollection { get; init; }
    public TermType? Type { get; set; }
    public Term? BaseTerm { get; set; }
}

/// <summary>
/// A resolved Function.
/// </summary>
public sealed class Function
{
    public required string Name { get; init; }
    public bool IsBound { get; init; }
    public bool IsComposable { get; init; }
    public IReadOnlyList<EntitySetPathSegment>? EntitySetPath { get; init; }
    public List<OperationParameter> Parameters { get; init; } = [];
    public OperationReturnType? ReturnType { get; init; }
}

/// <summary>
/// A resolved Action.
/// </summary>
public sealed class Action
{
    public required string Name { get; init; }
    public bool IsBound { get; init; }
    public IReadOnlyList<EntitySetPathSegment>? EntitySetPath { get; init; }
    public List<OperationParameter> Parameters { get; init; } = [];
    public OperationReturnType? ReturnType { get; init; }
}

/// <summary>
/// A resolved operation parameter.
/// </summary>
public sealed class OperationParameter
{
    public required string Name { get; init; }
    public required TermType Type { get; init; }
    public bool IsCollection { get; init; }
}

/// <summary>
/// A resolved operation return type.
/// </summary>
public sealed class OperationReturnType
{
    public required TermType Type { get; init; }
    public bool IsCollection { get; init; }
}

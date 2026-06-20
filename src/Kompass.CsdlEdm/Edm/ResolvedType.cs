namespace Kompass.CsdlEdm.Edm;

/// <summary>
/// Marker for types that can be the target of a structural property type reference.
/// Implemented by <see cref="PrimitiveType"/>, <see cref="ComplexType"/>,
/// <see cref="EnumType"/>, and <see cref="TypeDefinition"/>.
/// </summary>
public interface IPropertyType { }

/// <summary>
/// Marker for types that can be the target of a term, operation parameter, or return type reference.
/// Extends <see cref="IPropertyType"/> with <see cref="EntityType"/>.
/// </summary>
public interface ITermType { }

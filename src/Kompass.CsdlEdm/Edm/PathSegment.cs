namespace Kompass.CsdlEdm.Edm;

/// <summary>
/// One segment of a resolved binding path or target.
/// Uses direct object references (GC handles cycles, unlike Rust's Weak refs).
/// </summary>
public abstract class BindingPathSegment
{
    private BindingPathSegment() { }

    public abstract string DisplayName { get; }

    public sealed class PropertySegment(Property Property) : BindingPathSegment
    {
        public Property Property { get; } = Property;
        public override string DisplayName => Property.Name;
    }

    public sealed class NavigationPropertySegment(NavigationProperty NavigationProperty) : BindingPathSegment
    {
        public NavigationProperty NavigationProperty { get; } = NavigationProperty;
        public override string DisplayName => NavigationProperty.Name;
    }

    public sealed class EntityTypeCastSegment(EntityType EntityType) : BindingPathSegment
    {
        public EntityType EntityType { get; } = EntityType;
        public override string DisplayName => EntityType.Name;
    }

    public sealed class ComplexTypeCastSegment(ComplexType ComplexType) : BindingPathSegment
    {
        public ComplexType ComplexType { get; } = ComplexType;
        public override string DisplayName => ComplexType.Name;
    }

    public sealed class EntitySetSegment(EntitySet EntitySet) : BindingPathSegment
    {
        public EntitySet EntitySet { get; } = EntitySet;
        public override string DisplayName => EntitySet.Name;
    }

    public sealed class SingletonSegment(Singleton Singleton) : BindingPathSegment
    {
        public Singleton Singleton { get; } = Singleton;
        public override string DisplayName => Singleton.Name;
    }

    public sealed class EntityContainerSegment(EntityContainer EntityContainer) : BindingPathSegment
    {
        public EntityContainer EntityContainer { get; } = EntityContainer;
        public override string DisplayName => EntityContainer.Name;
    }

    public sealed class UnresolvedSegment(string Name) : BindingPathSegment
    {
        public string Name { get; } = Name;
        public override string DisplayName => Name;
    }

    public static string PathToString(IReadOnlyList<BindingPathSegment> path)
    {
        return string.Join("/", path.Select(s => s.DisplayName));
    }
}

/// <summary>
/// One segment of a resolved entity key path.
/// </summary>
public abstract class KeyPathSegment
{
    private KeyPathSegment() { }

    public abstract string DisplayName { get; }

    public sealed class PropertySegment(Property Property) : KeyPathSegment
    {
        public Property Property { get; } = Property;
        public override string DisplayName => Property.Name;
    }

    public sealed class UnresolvedSegment(string Name) : KeyPathSegment
    {
        public string Name { get; } = Name;
        public override string DisplayName => Name;
    }

    public static string PathToString(IReadOnlyList<KeyPathSegment> path)
    {
        return string.Join("/", path.Select(s => s.DisplayName));
    }
}

/// <summary>
/// One segment of a resolved operation EntitySetPath.
/// </summary>
public abstract class EntitySetPathSegment
{
    private EntitySetPathSegment() { }

    public abstract string DisplayName { get; }

    public sealed class BindingParameterSegment(string Name) : EntitySetPathSegment
    {
        public string Name { get; } = Name;
        public override string DisplayName => Name;
    }

    public sealed class NavigationPropertySegment(NavigationProperty NavigationProperty) : EntitySetPathSegment
    {
        public NavigationProperty NavigationProperty { get; } = NavigationProperty;
        public override string DisplayName => NavigationProperty.Name;
    }

    public sealed class PropertySegment(Property Property) : EntitySetPathSegment
    {
        public Property Property { get; } = Property;
        public override string DisplayName => Property.Name;
    }

    public sealed class UnresolvedSegment(string Name) : EntitySetPathSegment
    {
        public string Name { get; } = Name;
        public override string DisplayName => Name;
    }

    public static string PathToString(IReadOnlyList<EntitySetPathSegment> path)
    {
        return string.Join("/", path.Select(s => s.DisplayName));
    }
}

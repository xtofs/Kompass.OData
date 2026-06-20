namespace Kompass.CsdlEdm.Edm;

/// <summary>
/// A resolved entity container.
/// </summary>
public sealed class EntityContainer
{
    public required string Name { get; init; }
    public List<EntityContainerElement> Elements { get; init; } = [];
}

/// <summary>
/// A resolved element within an entity container. Sealed hierarchy.
/// </summary>
public abstract class EntityContainerElement
{
    private EntityContainerElement() { }

    public sealed class EntitySetElement(EntitySet EntitySet) : EntityContainerElement
    {
        public EntitySet EntitySet { get; } = EntitySet;
    }

    public sealed class SingletonElement(Singleton Singleton) : EntityContainerElement
    {
        public Singleton Singleton { get; } = Singleton;
    }

    public sealed class FunctionImportElement(FunctionImport FunctionImport) : EntityContainerElement
    {
        public FunctionImport FunctionImport { get; } = FunctionImport;
    }

    public sealed class ActionImportElement(ActionImport ActionImport) : EntityContainerElement
    {
        public ActionImport ActionImport { get; } = ActionImport;
    }
}

/// <summary>
/// A resolved entity set. Target is a direct reference to the resolved EntityType.
/// </summary>
public sealed class EntitySet
{
    public required string Name { get; init; }
    public required EntityType Target { get; init; }
    public List<NavigationPropertyBinding> NavigationPropertyBindings { get; set; } = [];
}

/// <summary>
/// A resolved singleton. Target is a direct reference to the resolved EntityType.
/// </summary>
public sealed class Singleton
{
    public required string Name { get; init; }
    public required EntityType Target { get; init; }
    public List<NavigationPropertyBinding> NavigationPropertyBindings { get; set; } = [];
}

/// <summary>
/// A resolved function import.
/// </summary>
public sealed class FunctionImport
{
    public required string Name { get; init; }
    public required string Function { get; init; }
    public IReadOnlyList<BindingPathSegment>? EntitySet { get; init; }
}

/// <summary>
/// A resolved action import.
/// </summary>
public sealed class ActionImport
{
    public required string Name { get; init; }
    public required string Action { get; init; }
    public IReadOnlyList<BindingPathSegment>? EntitySet { get; init; }
}

/// <summary>
/// A resolved navigation property binding with resolved path and target.
/// </summary>
public sealed class NavigationPropertyBinding
{
    public required IReadOnlyList<BindingPathSegment> Path { get; init; }
    public required IReadOnlyList<BindingPathSegment> Target { get; init; }

    public string PathString => BindingPathSegment.PathToString(Path);
    public string TargetString => BindingPathSegment.PathToString(Target);
}

namespace Kompass.CsdlEdm.Csdl;

/// <summary>
/// A CSDL EntityContainer declaration.
/// </summary>
public sealed class EntityContainer : SchemaElement
{
    public required override string Name { get; set; }
    public string? Extends { get; set; }
    public List<EntitySet> EntitySets { get; set; } = [];
    public List<Singleton> Singletons { get; set; } = [];
    public List<FunctionImport> FunctionImports { get; set; } = [];
    public List<ActionImport> ActionImports { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL EntitySet declaration within an EntityContainer.
/// </summary>
public sealed class EntitySet
{
    public required string Name { get; set; }
    public string? EntityType { get; set; }
    public bool? IncludeInServiceDocument { get; set; }
    public List<NavigationPropertyBinding> NavigationPropertyBindings { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL Singleton declaration within an EntityContainer.
/// </summary>
public sealed class Singleton
{
    public required string Name { get; set; }
    public string? TypeName { get; set; }
    public bool? IncludeInServiceDocument { get; set; }
    public List<NavigationPropertyBinding> NavigationPropertyBindings { get; set; } = [];
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL FunctionImport declaration within an EntityContainer.
/// </summary>
public sealed class FunctionImport
{
    public required string Name { get; set; }
    public string? Function { get; set; }
    public string? EntitySet { get; set; }
    public bool? IncludeInServiceDocument { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A CSDL ActionImport declaration within an EntityContainer.
/// </summary>
public sealed class ActionImport
{
    public required string Name { get; set; }
    public string? Action { get; set; }
    public string? EntitySet { get; set; }
    public bool? IncludeInServiceDocument { get; set; }
    public List<Annotation> Annotations { get; set; } = [];
}

/// <summary>
/// A navigation property binding within an EntitySet or Singleton.
/// </summary>
public sealed class NavigationPropertyBinding
{
    public required string Path { get; set; }
    public required string Target { get; set; }
}

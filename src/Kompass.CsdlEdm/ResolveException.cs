namespace Kompass.CsdlEdm;

/// <summary>
/// Errors that can occur during CSDL resolution.
/// </summary>
public class ResolveException : Exception
{
    public ResolveException(string message) : base(message) { }
}

public class UnknownTypeException(string typeName) : ResolveException($"Unknown type: '{typeName}'")
{
    public string TypeName { get; } = typeName;
}

public class UnknownEntityException(string entityName) : ResolveException($"Unknown entity: '{entityName}'")
{
    public string EntityName { get; } = entityName;
}

public class DuplicateNameException(string name) : ResolveException($"Duplicate name: '{name}'")
{
    public string DuplicateName { get; } = name;
}

public class MissingTypeNameException(string elementKind, string elementName) : ResolveException($"Missing type name on {elementKind} '{elementName}'")
{
    public string ElementKind { get; } = elementKind;
    public string ElementName { get; } = elementName;
}

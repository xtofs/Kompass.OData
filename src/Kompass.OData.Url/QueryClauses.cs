namespace Kompass.OData.Url;

/// <summary>
/// Represents the $select query option.
/// </summary>
public sealed class SelectClause
{
    public IReadOnlyList<string> Items { get; }

    public SelectClause(IReadOnlyList<string> items)
    {
        Items = items;
    }

    public override string ToString()
    {
        return string.Join(",", Items);
    }
}

/// <summary>
/// Represents the $expand query option.
/// </summary>
public sealed class ExpandClause
{
    public IReadOnlyList<string> Items { get; }

    public ExpandClause(IReadOnlyList<string> items)
    {
        Items = items;
    }

    public override string ToString()
    {
        return string.Join(",", Items);
    }
}

/// <summary>
/// Represents the $orderby query option.
/// </summary>
public sealed class OrderByClause
{
    public string Expression { get; }

    public OrderByClause(string expression)
    {
        Expression = expression;
    }

    public override string ToString()
    {
        return Expression;
    }
}

/// <summary>
/// Represents $top and $skip paging options.
/// </summary>
public sealed class Page
{
    public long? Top { get; }
    public long? Skip { get; }

    public static Page Empty { get; } = new Page(null, null);

    public Page(long? top, long? skip)
    {
        Top = top;
        Skip = skip;
    }

    public bool IsEmpty => Top is null && Skip is null;
}

namespace Kompass.OData.Url;

/// <summary>
/// The subset of parsed query options that handlers typically need (no resource path).
/// </summary>
public sealed class QueryOptions
{
    public SelectClause? Select { get; init; }
    public FilterClause? Filter { get; init; }
    public ExpandClause? Expand { get; init; }
    public Page Page { get; init; } = Page.Empty;
    public OrderByClause? OrderBy { get; init; }
    public bool? Count { get; init; }
    public IReadOnlyDictionary<string, List<string>> Custom { get; init; } =
        new Dictionary<string, List<string>>();

    /// <summary>
    /// Parse system query options from a raw query string (without the leading '?').
    /// </summary>
    public static QueryOptions Parse(string queryString)
    {
        return QueryStringParser.ParseQueryOptions(queryString);
    }
}

namespace Kompass.OData.Url;

/// <summary>
/// A fully parsed OData URL, including resource path, path markers, and query options.
/// </summary>
public sealed class ODataQuery
{
    public Uri Url { get; }
    public ResourcePath ResourcePath { get; }
    public bool Each { get; init; }
    public bool Count { get; init; }
    public bool Ref { get; init; }
    public bool Value { get; init; }
    public SelectClause? Select { get; init; }
    public FilterClause? Filter { get; init; }
    public ExpandClause? Expand { get; init; }
    public Page Page { get; init; } = Page.Empty;
    public OrderByClause? OrderBy { get; init; }
    public bool? InlineCount { get; init; }
    public IReadOnlyDictionary<string, List<string>> Custom { get; init; } =
        new Dictionary<string, List<string>>();
    public string? Fragment { get; init; }

    private ODataQuery(Uri url, ResourcePath resourcePath)
    {
        Url = url;
        ResourcePath = resourcePath;
    }

    /// <summary>
    /// Parse an OData URL string into an <see cref="ODataQuery"/>.
    /// </summary>
    public static ODataQuery Parse(string input)
    {
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            throw new ODataParseException(new ParseError.InvalidUrl(input));
        }

        return FromUri(uri);
    }

    /// <summary>
    /// Parse an OData URL from a <see cref="Uri"/> instance.
    /// </summary>
    public static ODataQuery FromUri(Uri uri)
    {
        // Extract resource path and path markers
        var pathSegments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        var each = false;
        var count = false;
        var refFlag = false;
        var value = false;

        // Remove path markers from segments
        var cleanSegments = new List<string>();
        foreach (var seg in pathSegments)
        {
            switch (seg)
            {
                case "$each":
                    each = true;
                    break;
                case "$count":
                    count = true;
                    break;
                case "$ref":
                    refFlag = true;
                    break;
                case "$value":
                    value = true;
                    break;
                default:
                    cleanSegments.Add(seg);
                    break;
            }
        }

        var resourcePath = new ResourcePath(cleanSegments);

        // Parse query options
        var queryString = uri.Query.TrimStart('?');
        var options = QueryStringParser.ParseQueryOptions(queryString);

        // Extract fragment
        var fragment = string.IsNullOrEmpty(uri.Fragment)
            ? null
            : uri.Fragment.TrimStart('#');

        return new ODataQuery(uri, resourcePath)
        {
            Each = each,
            Count = count,
            Ref = refFlag,
            Value = value,
            Select = options.Select,
            Filter = options.Filter,
            Expand = options.Expand,
            Page = options.Page,
            OrderBy = options.OrderBy,
            InlineCount = options.Count,
            Custom = options.Custom,
            Fragment = fragment,
        };
    }

    /// <summary>
    /// Convert to <see cref="QueryOptions"/> (dropping path information).
    /// </summary>
    public QueryOptions ToQueryOptions()
    {
        return new QueryOptions
        {
            Select = Select,
            Filter = Filter,
            Expand = Expand,
            Page = Page,
            OrderBy = OrderBy,
            Count = InlineCount,
            Custom = Custom,
        };
    }
}

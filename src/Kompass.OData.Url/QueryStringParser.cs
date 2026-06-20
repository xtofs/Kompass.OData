namespace Kompass.OData.Url;

/// <summary>
/// Internal helper that parses a query string into <see cref="QueryOptions"/>.
/// </summary>
internal static class QueryStringParser
{
    internal static QueryOptions ParseQueryOptions(string queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return new QueryOptions();
        }

        SelectClause? select = null;
        FilterClause? filter = null;
        ExpandClause? expand = null;
        OrderByClause? orderBy = null;
        long? top = null;
        long? skip = null;
        bool? count = null;
        var custom = new Dictionary<string, List<string>>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in SplitQueryString(queryString))
        {
            var key = pair.Key;
            var value = pair.Value;

            switch (key)
            {
                case "$select":
                    EnsureNoDuplicate(seen, "select");
                    select = new SelectClause(
                        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;

                case "$filter":
                    EnsureNoDuplicate(seen, "filter");
                    var parser = new FilterParser(value);
                    var expr = parser.ParseExpression();
                    filter = new FilterClause(expr);
                    break;

                case "$expand":
                    EnsureNoDuplicate(seen, "expand");
                    expand = new ExpandClause(
                        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;

                case "$orderby":
                    EnsureNoDuplicate(seen, "orderby");
                    orderBy = new OrderByClause(value);
                    break;

                case "$top":
                    EnsureNoDuplicate(seen, "top");
                    if (!long.TryParse(value, out var topVal))
                    {
                        throw new ODataParseException(
                            new ParseError.InvalidInteger("top", value));
                    }
                    top = topVal;
                    break;

                case "$skip":
                    EnsureNoDuplicate(seen, "skip");
                    if (!long.TryParse(value, out var skipVal))
                    {
                        throw new ODataParseException(
                            new ParseError.InvalidInteger("skip", value));
                    }
                    skip = skipVal;
                    break;

                case "$count":
                    EnsureNoDuplicate(seen, "inlinecount");
                    if (!bool.TryParse(value, out var countVal))
                    {
                        throw new ODataParseException(
                            new ParseError.InvalidBoolean("inlinecount", value));
                    }
                    count = countVal;
                    break;

                default:
                    // Custom query option (non-$ prefixed or unrecognized $-prefixed)
                    if (!custom.TryGetValue(key, out var list))
                    {
                        list = new List<string>();
                        custom[key] = list;
                    }
                    list.Add(value);
                    break;
            }
        }

        return new QueryOptions
        {
            Select = select,
            Filter = filter,
            Expand = expand,
            Page = new Page(top, skip),
            OrderBy = orderBy,
            Count = count,
            Custom = custom,
        };
    }

    private static void EnsureNoDuplicate(HashSet<string> seen, string option)
    {
        if (!seen.Add(option))
        {
            throw new ODataParseException(
                new ParseError.DuplicateQueryOption(option));
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> SplitQueryString(string queryString)
    {
        foreach (var part in queryString.Split('&'))
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            var eqIdx = part.IndexOf('=');
            if (eqIdx < 0)
            {
                yield return new KeyValuePair<string, string>(
                    Uri.UnescapeDataString(part), "");
            }
            else
            {
                var key = Uri.UnescapeDataString(part[..eqIdx]);
                var value = Uri.UnescapeDataString(part[(eqIdx + 1)..]);
                yield return new KeyValuePair<string, string>(key, value);
            }
        }
    }
}

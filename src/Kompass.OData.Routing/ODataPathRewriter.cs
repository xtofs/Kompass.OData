namespace Kompass.OData.Routing;

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Rewrites OData subsegment-key URLs into segment form for standard routing.
/// <c>/Rooms('oak-204')/Printers('hp-42')</c> becomes
/// <c>/Rooms/__key__/oak-204/Printers/__key__/hp-42</c>.
/// </summary>
public static partial class ODataPathRewriter
{
    /// <summary>
    /// The sentinel segment that marks a key value. Never collides with valid OData identifiers
    /// because OData identifiers cannot start with underscores.
    /// </summary>
    public const string KeySegment = "__key__";

    // Matches segments like: Name('value') or Name(42) or Name()
    private static readonly Regex KeyPattern = new Regex(
        @"([^(/]+)\(([^)]*)\)",
        RegexOptions.Compiled);

    // Reverse: matches /Name/__key__/{param} in route patterns
    [GeneratedRegex(@"/([^/]+)/__key__/(\{[^}]+\})", RegexOptions.Compiled)]
    private static partial Regex ReverseKeyPattern();

    /// <summary>
    /// Converts an internal route pattern back to OData parenthesized form.
    /// <c>/Rooms/__key__/{id}/Printers/__key__/{navId}</c> becomes
    /// <c>/Rooms({id})/Printers({navId})</c>.
    /// </summary>
    public static string FormatAsODataPath(string routePattern)
    {
        return ReverseKeyPattern().Replace(routePattern, "/$1($2)");
    }

    /// <summary>
    /// Rewrite an OData path, converting inline keys to segment form.
    /// </summary>
    public static string RewritePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return path;
        }

        var sb = new StringBuilder();
        var segments = path.Split('/');

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];

            if (i > 0)
            {
                sb.Append('/');
            }

            var match = KeyPattern.Match(segment);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var rawKey = match.Groups[2].Value;
                var key = StripQuotes(rawKey);
                sb.Append(name);
                sb.Append('/');
                sb.Append(KeySegment);
                sb.Append('/');
                sb.Append(key);
            }
            else
            {
                sb.Append(segment);
            }
        }

        return sb.ToString();
    }

    private static string StripQuotes(string key)
    {
        if (key.Length >= 2 && key[0] == '\'' && key[^1] == '\'')
        {
            return key[1..^1];
        }
        return key;
    }
}

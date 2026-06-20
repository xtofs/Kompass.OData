namespace Kompass.OData.Url;

/// <summary>
/// Errors that can occur when parsing an OData URL or query string.
/// </summary>
public abstract record ParseError(string Message)
{
    public sealed record InvalidUrl(string Value)
        : ParseError($"invalid url: {Value}");

    public sealed record DuplicateQueryOption(string Option)
        : ParseError($"duplicate OData query option: {Option}");

    public sealed record InvalidInteger(string Option, string Value)
        : ParseError($"invalid integer for {Option}: {Value}");

    public sealed record InvalidBoolean(string Option, string Value)
        : ParseError($"invalid boolean for {Option}: {Value}");

    public sealed record InvalidFilterExpression(string Value, int Position, string Detail)
        : ParseError($"invalid filter expression at position {Position}: {Detail}");
}

/// <summary>
/// Exception wrapper for <see cref="ParseError"/>.
/// </summary>
public class ODataParseException : Exception
{
    public ParseError Error { get; }

    public ODataParseException(ParseError error) : base(error.Message)
    {
        Error = error;
    }
}

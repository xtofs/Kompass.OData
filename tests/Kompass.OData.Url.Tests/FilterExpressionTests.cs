namespace Kompass.OData.Url.Tests;

using Kompass.OData.Url;
using Xunit;

public class FilterExpressionTests
{
    private static FilterExpression ParseFilter(string filter)
    {
        var query = ODataQuery.Parse(
            $"https://example.test/X?$filter={Uri.EscapeDataString(filter)}");
        return query.Filter!.Expression;
    }

    [Fact]
    public void ParsesPrecedenceCorrectly()
    {
        // Price add 2 mul 3 gt 10 and not (Discontinued eq true)
        var expr = ParseFilter("Price add 2 mul 3 gt 10 and not (Discontinued eq true)");
        Assert.IsType<FilterExpressionKind.Binary>(expr.Kind);
        var root = (FilterExpressionKind.Binary)expr.Kind;
        Assert.Equal(FilterBinaryOperator.And, root.Operator);
    }

    [Fact]
    public void ParsesFunctionCallsAndPaths()
    {
        var expr = ParseFilter("contains(tolower(Name),'acme') or Orders/anyCount gt 0");
        Assert.IsType<FilterExpressionKind.Binary>(expr.Kind);
        var root = (FilterExpressionKind.Binary)expr.Kind;
        Assert.Equal(FilterBinaryOperator.Or, root.Operator);

        // Left: function call
        Assert.IsType<FilterExpressionKind.FunctionCall>(root.Left.Kind);
        var fc = (FilterExpressionKind.FunctionCall)root.Left.Kind;
        Assert.Equal("contains", fc.Data.Name);
        Assert.Equal(2, fc.Data.Arguments.Count);

        // Right: binary gt with member path
        Assert.IsType<FilterExpressionKind.Binary>(root.Right.Kind);
        var rightBin = (FilterExpressionKind.Binary)root.Right.Kind;
        Assert.Equal(FilterBinaryOperator.GreaterThan, rightBin.Operator);
        Assert.IsType<FilterExpressionKind.Member>(rightBin.Left.Kind);
        var memberPath = (FilterExpressionKind.Member)rightBin.Left.Kind;
        Assert.Equal(new[] { "Orders", "anyCount" }, memberPath.Path.Segments);
    }

    [Fact]
    public void TracksFilterNodeSpans()
    {
        // (Rating gt 5) and Name eq 'Bob'
        var expr = ParseFilter("(Rating gt 5) and Name eq 'Bob'");

        // Root spans entire expression
        Assert.Equal(0, expr.Span.Start);
        Assert.Equal(31, expr.Span.End);

        // Left side: (Rating gt 5) — parenthesized
        var root = (FilterExpressionKind.Binary)expr.Kind;
        Assert.Equal(0, root.Left.Span.Start);

        // Right side: starts after "and "
        Assert.Equal(31, root.Right.Span.End);
    }

    [Fact]
    public void RejectsInvalidFilterExpressions()
    {
        var ex = Assert.Throws<ODataParseException>(
            () => ODataQuery.Parse("https://example.test/Customers?$filter=Price%20gt"));

        Assert.IsType<ParseError.InvalidFilterExpression>(ex.Error);
    }

    [Fact]
    public void DisplayKeepsAndOrPrecedenceWithParentheses()
    {
        var expr = ParseFilter("(A eq 1 or B eq 2) and C eq 3");
        Assert.Equal("(A eq 1 or B eq 2) and C eq 3", expr.ToString());
    }

    [Fact]
    public void DisplayFormatsUnaryNotAsKeyword()
    {
        var expr = ParseFilter("not (A eq 1)");
        Assert.Equal("not (A eq 1)", expr.ToString());
    }

    [Fact]
    public void DisplayEscapesSingleQuotesInStringLiterals()
    {
        var expr = ParseFilter("Name eq 'O''Brien'");
        Assert.Equal("Name eq 'O''Brien'", expr.ToString());
    }

    [Theory]
    [InlineData("A eq 1")]
    [InlineData("A gt 5 and B lt 10")]
    [InlineData("contains(Name, 'test')")]
    [InlineData("Price mul 2 add 1 gt 100")]
    public void DisplayRoundtrip(string filter)
    {
        var expr = ParseFilter(filter);
        Assert.Equal(filter, expr.ToString());
    }

    [Fact]
    public void ParsesNestedFunctionCalls()
    {
        var expr = ParseFilter("contains(tolower(Name),'acme')");
        Assert.IsType<FilterExpressionKind.FunctionCall>(expr.Kind);
        var fc = (FilterExpressionKind.FunctionCall)expr.Kind;
        Assert.Equal("contains", fc.Data.Name);

        // First argument is another function call
        Assert.IsType<FilterExpressionKind.FunctionCall>(fc.Data.Arguments[0].Kind);
        var inner = (FilterExpressionKind.FunctionCall)fc.Data.Arguments[0].Kind;
        Assert.Equal("tolower", inner.Data.Name);
    }

    [Fact]
    public void ParsesNullLiteral()
    {
        var expr = ParseFilter("Name eq null");
        var bin = (FilterExpressionKind.Binary)expr.Kind;
        Assert.IsType<FilterExpressionKind.Literal>(bin.Right.Kind);
        var lit = (FilterExpressionKind.Literal)bin.Right.Kind;
        Assert.IsType<FilterLiteral.Null>(lit.Value);
    }

    [Fact]
    public void ParsesBooleanLiterals()
    {
        var expr = ParseFilter("Active eq true");
        var bin = (FilterExpressionKind.Binary)expr.Kind;
        var lit = (FilterExpressionKind.Literal)bin.Right.Kind;
        Assert.IsType<FilterLiteral.Boolean>(lit.Value);
        Assert.True(((FilterLiteral.Boolean)lit.Value).Value);
    }
}

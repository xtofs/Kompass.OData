namespace Kompass.OData.Url.Tests;

using Kompass.OData.Url;
using Xunit;

public class ODataQueryTests
{
    [Fact]
    public void ParsesFullQueryWithAllOptions()
    {
        var query = ODataQuery.Parse(
            "https://example.test/odata/People(1)/Orders?$select=Id,Name&$filter=Rating%20gt%205&$expand=Items,Customer&$top=10&$skip=3&$orderby=Name%20desc&$count=true&x-custom=abc#anchor");

        Assert.Equal(new[] { "odata", "People(1)", "Orders" }, query.ResourcePath.Segments);
        Assert.False(query.Each);
        Assert.False(query.Count);
        Assert.False(query.Ref);
        Assert.False(query.Value);

        Assert.NotNull(query.Select);
        Assert.Equal(new[] { "Id", "Name" }, query.Select!.Items);

        Assert.NotNull(query.Filter);
        var filterKind = query.Filter!.Expression.Kind;
        Assert.IsType<FilterExpressionKind.Binary>(filterKind);
        var bin = (FilterExpressionKind.Binary)filterKind;
        Assert.Equal(FilterBinaryOperator.GreaterThan, bin.Operator);

        Assert.NotNull(query.Expand);
        Assert.Equal(new[] { "Items", "Customer" }, query.Expand!.Items);

        Assert.Equal(10, query.Page.Top);
        Assert.Equal(3, query.Page.Skip);

        Assert.NotNull(query.OrderBy);
        Assert.Equal("Name desc", query.OrderBy!.Expression);

        Assert.True(query.InlineCount);

        Assert.True(query.Custom.ContainsKey("x-custom"));
        Assert.Equal(new[] { "abc" }, query.Custom["x-custom"]);

        Assert.Equal("anchor", query.Fragment);
    }

    [Fact]
    public void ParsesPathMarkersAsFlags()
    {
        var query = ODataQuery.Parse(
            "https://example.test/Customers/$count/$ref/$value/$each");

        Assert.Equal(new[] { "Customers" }, query.ResourcePath.Segments);
        Assert.True(query.Count);
        Assert.True(query.Ref);
        Assert.True(query.Value);
        Assert.True(query.Each);
    }

    [Fact]
    public void RejectsInvalidUrls()
    {
        var ex = Assert.Throws<ODataParseException>(
            () => ODataQuery.Parse("not a url"));

        Assert.IsType<ParseError.InvalidUrl>(ex.Error);
    }

    [Theory]
    [InlineData("https://example.test/Customers?$count=maybe", "inlinecount", "maybe")]
    public void RejectsInvalidBoolean(string url, string option, string value)
    {
        var ex = Assert.Throws<ODataParseException>(
            () => ODataQuery.Parse(url));

        var err = Assert.IsType<ParseError.InvalidBoolean>(ex.Error);
        Assert.Equal(option, err.Option);
        Assert.Equal(value, err.Value);
    }

    [Fact]
    public void RejectsDuplicateOption()
    {
        var ex = Assert.Throws<ODataParseException>(
            () => ODataQuery.Parse("https://example.test/Customers?$top=1&$top=2"));

        var err = Assert.IsType<ParseError.DuplicateQueryOption>(ex.Error);
        Assert.Equal("top", err.Option);
    }

    [Fact]
    public void ConvertToQueryOptions()
    {
        var query = ODataQuery.Parse(
            "https://example.test/Customers?$select=Id&$top=5&$skip=10");

        var opts = query.ToQueryOptions();
        Assert.NotNull(opts.Select);
        Assert.Equal(new[] { "Id" }, opts.Select!.Items);
        Assert.Equal(5, opts.Page.Top);
        Assert.Equal(10, opts.Page.Skip);
    }
}

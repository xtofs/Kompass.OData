namespace Kompass.OData.Routing.Tests;

using Kompass.OData.Routing;
using Xunit;

public class PathRewriteTests
{
    [Theory]
    [InlineData("/Rooms", "/Rooms")]
    [InlineData("/Rooms('oak-204')", "/Rooms/__key__/oak-204")]
    [InlineData("/Rooms(42)", "/Rooms/__key__/42")]
    [InlineData("/Rooms('oak-204')/Printers('hp-42')", "/Rooms/__key__/oak-204/Printers/__key__/hp-42")]
    [InlineData("/Rooms('oak-204')/Printers", "/Rooms/__key__/oak-204/Printers")]
    [InlineData("/Rooms('oak-204')/$count", "/Rooms/__key__/oak-204/$count")]
    [InlineData("/Rooms/$count", "/Rooms/$count")]
    [InlineData("/", "/")]
    [InlineData("/Rooms()", "/Rooms/__key__/")]
    [InlineData("/Rooms('it''s')", "/Rooms/__key__/it''s")]
    [InlineData("/A('1')/B('2')/C", "/A/__key__/1/B/__key__/2/C")]
    public void RewritesPath(string input, string expected)
    {
        var result = ODataPathRewriter.RewritePath(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void KeySegmentConstantNeverCollidesWithODataIdentifiers()
    {
        // OData identifiers cannot start with underscore
        Assert.StartsWith("__", ODataPathRewriter.KeySegment);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    public void EmptyAndRootPathUnchanged(string input)
    {
        Assert.Equal(input, ODataPathRewriter.RewritePath(input));
    }
}

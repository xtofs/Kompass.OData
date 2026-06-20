namespace Kompass.OData.Service.Tests;

using Xunit;

public class ContextUrlBuilderTests
{
    private readonly ContextUrlBuilder _builder = ContextUrlBuilder.Default;

    // ── Entity set collection ──────────────────────────────────────────────

    [Fact]
    public void EntitySetCollection_NoSelect_ReturnsPlainFragment()
    {
        var result = _builder.ForEntitySet("Products", SerializationShape.Collection);

        Assert.Equal("$metadata#Products", result);
    }

    [Fact]
    public void EntitySetEntity_NoSelect_ReturnsEntityFragment()
    {
        var result = _builder.ForEntitySet("Products", SerializationShape.Entity);

        Assert.Equal("$metadata#Products/$entity", result);
    }

    // ── $select projection ─────────────────────────────────────────────────

    [Theory]
    [InlineData(new[] { "Name" }, "$metadata#Products(Name)")]
    [InlineData(new[] { "Name", "Price" }, "$metadata#Products(Name,Price)")]
    public void EntitySetCollection_WithSelect_IncludesProjection(string[] properties, string expected)
    {
        var shape = SerializationShape.CollectionWithSelect(properties);

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new[] { "Name" }, "$metadata#Products(Name)/$entity")]
    [InlineData(new[] { "Name", "Price" }, "$metadata#Products(Name,Price)/$entity")]
    public void EntitySetEntity_WithSelect_IncludesProjectionBeforeEntity(string[] properties, string expected)
    {
        var shape = SerializationShape.EntityWithSelect(properties);

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal(expected, result);
    }

    // ── Contained navigation ───────────────────────────────────────────────

    [Fact]
    public void ContainedNavCollection_NoSelect_ReturnsPathFragment()
    {
        var result = _builder.ForContainedNavigation("Customers", "Orders", SerializationShape.Collection);

        Assert.Equal("$metadata#Customers/Orders", result);
    }

    [Fact]
    public void ContainedNavEntity_NoSelect_ReturnsPathEntityFragment()
    {
        var result = _builder.ForContainedNavigation("Customers", "Orders", SerializationShape.Entity);

        Assert.Equal("$metadata#Customers/Orders/$entity", result);
    }

    [Fact]
    public void ContainedNavCollection_WithSelect_IncludesProjection()
    {
        var shape = SerializationShape.CollectionWithSelect(["Id", "Status"]);

        var result = _builder.ForContainedNavigation("Customers", "Orders", shape);

        Assert.Equal("$metadata#Customers/Orders(Id,Status)", result);
    }

    [Fact]
    public void ContainedNavEntity_WithSelect_IncludesProjectionBeforeEntity()
    {
        var shape = SerializationShape.EntityWithSelect(["Id"]);

        var result = _builder.ForContainedNavigation("Customers", "Orders", shape);

        Assert.Equal("$metadata#Customers/Orders(Id)/$entity", result);
    }

    // ── No parent key in contained nav context URL ─────────────────────────

    [Fact]
    public void ContainedNav_DoesNotIncludeParentKey()
    {
        var result = _builder.ForContainedNavigation("Customers", "Orders", SerializationShape.Collection);

        Assert.DoesNotContain("(", result);
        Assert.DoesNotContain(")", result);
    }

    // ── Singleton ──────────────────────────────────────────────────────────

    [Fact]
    public void Singleton_ReturnsSimpleFragment()
    {
        var result = _builder.ForSingleton("Me", SerializationShape.Entity);

        Assert.Equal("$metadata#Me/$entity", result);
    }

    // ── $ref ───────────────────────────────────────────────────────────────

    [Fact]
    public void EntitySet_Reference_ReturnsRefFragment()
    {
        var shape = new SerializationShape { IsCollection = true, IsReference = true };

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal("$metadata#Products/$ref", result);
    }

    [Fact]
    public void EntitySet_SingleReference_ReturnsRefEntityFragment()
    {
        var shape = new SerializationShape { IsSingleEntity = true, IsReference = true };

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal("$metadata#Products/$ref/$entity", result);
    }

    // ── $delta ─────────────────────────────────────────────────────────────

    [Fact]
    public void EntitySet_Delta_ReturnsDeltaFragment()
    {
        var shape = new SerializationShape { IsCollection = true, IsDelta = true };

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal("$metadata#Products/$delta", result);
    }

    // ── Derived type ───────────────────────────────────────────────────────

    [Fact]
    public void EntitySet_DerivedType_IncludesTypeQualifier()
    {
        var shape = new SerializationShape
        {
            IsCollection = true,
            DerivedTypeName = "Namespace.Book",
        };

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal("$metadata#Products/Namespace.Book", result);
    }

    [Fact]
    public void EntitySet_DerivedTypeWithSelect_IncludesBoth()
    {
        var shape = new SerializationShape
        {
            IsSingleEntity = true,
            DerivedTypeName = "Namespace.Book",
            SelectedProperties = ["Title", "ISBN"],
        };

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal("$metadata#Products/Namespace.Book(Title,ISBN)/$entity", result);
    }

    // ── Property ───────────────────────────────────────────────────────────

    [Fact]
    public void Property_ReturnsEntityPathPlusPropertyName()
    {
        var result = _builder.ForProperty("Products(1)", "Name");

        Assert.Equal("$metadata#Products(1)/Name", result);
    }

    // ── Custom metadata URI ────────────────────────────────────────────────

    [Fact]
    public void CustomMetadataUri_UsedAsPrefix()
    {
        var builder = new ContextUrlBuilder("https://host/service/$metadata");

        var result = builder.ForEntitySet("Products", SerializationShape.Collection);

        Assert.Equal("https://host/service/$metadata#Products", result);
    }

    // ── Suffix ordering ────────────────────────────────────────────────────

    [Fact]
    public void SuffixOrder_SelectThenRefThenDeltaThenEntity()
    {
        var shape = new SerializationShape
        {
            IsSingleEntity = true,
            IsReference = true,
            IsDelta = true,
            SelectedProperties = ["Name"],
        };

        var result = _builder.ForEntitySet("Products", shape);

        Assert.Equal("$metadata#Products(Name)/$ref/$delta/$entity", result);
    }
}

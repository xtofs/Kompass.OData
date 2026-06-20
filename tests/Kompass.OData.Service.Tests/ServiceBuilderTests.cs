namespace Kompass.OData.Service.Tests;

using Microsoft.AspNetCore.Http;
using Kompass.OData.Service;
using Xunit;

public class ServiceBuilderTests
{
    private const string RoomsCsdl = """
        <?xml version="1.0" encoding="utf-8"?>
        <edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
          <edmx:DataServices>
            <Schema Namespace="BuildingManagement" Alias="Bm" xmlns="http://docs.oasis-open.org/odata/ns/edm">
              <EntityType Name="Room">
                <Key><PropertyRef Name="Id" /></Key>
                <Property Name="Id" Type="Edm.String" Nullable="false" />
                <Property Name="Name" Type="Edm.String" />
                <NavigationProperty Name="Printers" Type="Collection(BuildingManagement.Printer)" ContainsTarget="true" />
                <NavigationProperty Name="Phones" Type="Collection(BuildingManagement.Phone)" ContainsTarget="true" />
              </EntityType>
              <EntityType Name="Printer">
                <Key><PropertyRef Name="Id" /></Key>
                <Property Name="Id" Type="Edm.String" Nullable="false" />
                <Property Name="Model" Type="Edm.String" />
              </EntityType>
              <EntityType Name="Phone">
                <Key><PropertyRef Name="Id" /></Key>
                <Property Name="Id" Type="Edm.String" Nullable="false" />
                <Property Name="Number" Type="Edm.String" />
                <Property Name="Extension" Type="Edm.String" />
              </EntityType>
              <EntityContainer Name="Container">
                <EntitySet Name="Rooms" EntityType="BuildingManagement.Room" />
              </EntityContainer>
            </Schema>
          </edmx:DataServices>
        </edmx:Edmx>
        """;

    [Fact]
    public void ThrowsOnUnknownEntitySet()
    {
        var builder = ODataServiceBuilder.FromCsdl(RoomsCsdl);

        Assert.Throws<InvalidOperationException>(() =>
            builder.EntitySet("NonExistent", es => es
                .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok()))));
    }

    [Fact]
    public void ThrowsOnInvalidContainedNav()
    {
        var builder = ODataServiceBuilder.FromCsdl(RoomsCsdl);

        Assert.Throws<InvalidOperationException>(() =>
            builder.EntitySet("Rooms", es => es
                .ContainedCollection("NonExistent", nav => nav
                    .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok())))));
    }

    [Fact]
    public void DetectsUnregisteredEntitySets()
    {
        var builder = ODataServiceBuilder.FromCsdl(RoomsCsdl);
        // Register nothing

        var warnings = builder.GetWarnings();
        Assert.Contains(warnings, w => w.Contains("Rooms") && w.Contains("no registered handlers"));
    }

    [Fact]
    public void DetectsUnregisteredContainedNavs()
    {
        var builder = ODataServiceBuilder.FromCsdl(RoomsCsdl);
        builder.EntitySet("Rooms", es => es
            .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok())));

        var warnings = builder.GetWarnings();
        Assert.Contains(warnings, w => w.Contains("Printers") && w.Contains("no registered handlers"));
        Assert.Contains(warnings, w => w.Contains("Phones") && w.Contains("no registered handlers"));
    }

    [Fact]
    public void NoWarningsWhenFullyRegistered()
    {
        var builder = ODataServiceBuilder.FromCsdl(RoomsCsdl);
        builder.EntitySet("Rooms", es => es
            .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok()))
            .OnGet((ctx, sp) => Task.FromResult<IResult>(Results.Ok()))
            .ContainedCollection("Printers", nav => nav
                .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok())))
            .ContainedCollection("Phones", nav => nav
                .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok()))));

        var warnings = builder.GetWarnings();
        Assert.Empty(warnings);
    }

    [Fact]
    public void AcceptsValidContainedNavigation()
    {
        var builder = ODataServiceBuilder.FromCsdl(RoomsCsdl);

        // Should not throw - Printers is a valid contained nav
        builder.EntitySet("Rooms", es => es
            .ContainedCollection("Printers", nav => nav
                .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok()))));
    }

    [Fact]
    public void GeneratesServiceDocument()
    {
        var builder = ODataServiceBuilder.FromCsdl(RoomsCsdl);
        builder.EntitySet("Rooms", es => es
            .OnList((ctx, sp) => Task.FromResult<IResult>(Results.Ok())));

        var doc = builder.GenerateServiceDocument("https://localhost:5000");
        Assert.Contains("Rooms", doc);
        Assert.Contains("EntitySet", doc);
    }
}

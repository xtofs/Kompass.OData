namespace Kompass.CsdlEdm.Tests;

using Kompass.CsdlEdm;
using Kompass.CsdlEdm.Csdl;
using Xunit;

public class CsdlXmlReaderTests
{
    private static string LoadFixture(string name)
    {
        var path = Path.Combine("Fixtures", name);
        return File.ReadAllText(path);
    }

    [Fact]
    public void ReadsRoomsFixture_EntityTypes()
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);

        Assert.NotNull(doc.Edmx);
        Assert.Equal("4.0", doc.Edmx!.Version);
        Assert.Single(doc.Edmx.Schemas);

        var schema = doc.Edmx.Schemas[0];
        Assert.Equal("BuildingManagement", schema.Namespace);
        Assert.Equal("Bm", schema.Alias);

        // 3 entity types + 1 container = 4 elements
        Assert.Equal(4, schema.Elements.Count);

        var room = schema.Elements.OfType<EntityType>()
            .First(e => e.Name == "Room");
        Assert.NotNull(room.Key);
        Assert.Single(room.Key!.PropertyRefs);
        Assert.Equal("Id", room.Key.PropertyRefs[0].Name);
        Assert.Equal(2, room.Properties.Count);
        Assert.Equal(2, room.NavigationProperties.Count);
    }

    [Theory]
    [InlineData("Room", 2, 2)]
    [InlineData("Printer", 2, 0)]
    [InlineData("Phone", 3, 0)]
    public void ReadsRoomsFixture_PropertyCounts(string typeName, int propCount, int navCount)
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var schema = doc.Edmx!.Schemas[0];

        var entityType = schema.Elements.OfType<EntityType>()
            .First(e => e.Name == typeName);

        Assert.Equal(propCount, entityType.Properties.Count);
        Assert.Equal(navCount, entityType.NavigationProperties.Count);
    }

    [Fact]
    public void ReadsRoomsFixture_ContainedNavigation()
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var schema = doc.Edmx!.Schemas[0];

        var room = schema.Elements.OfType<EntityType>()
            .First(e => e.Name == "Room");

        var printers = room.NavigationProperties.First(n => n.Name == "Printers");
        Assert.True(printers.ContainsTarget);
        Assert.True(printers.IsCollection);
        Assert.Equal("BuildingManagement.Printer", printers.TypeName);
    }

    [Fact]
    public void ReadsRoomsFixture_EntityContainer()
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var schema = doc.Edmx!.Schemas[0];

        var container = schema.Elements.OfType<EntityContainer>()
            .First();
        Assert.Equal("Container", container.Name);
        Assert.Single(container.EntitySets);
        Assert.Equal("Rooms", container.EntitySets[0].Name);
        Assert.Equal("BuildingManagement.Room", container.EntitySets[0].EntityType);
    }

    [Theory]
    [InlineData("Collection(Edm.String)", "Edm.String", true)]
    [InlineData("Edm.Int32", "Edm.Int32", false)]
    [InlineData("Collection(MyNamespace.MyType)", "MyNamespace.MyType", true)]
    [InlineData(null, null, false)]
    public void ParseTypeAttribute_HandlesCollectionWrapper(string? input, string? expectedType, bool expectedCollection)
    {
        var (typeName, isCollection) = CsdlXmlReader.ParseTypeAttribute(input);
        Assert.Equal(expectedType, typeName);
        Assert.Equal(expectedCollection, isCollection);
    }

    [Fact]
    public void ReadsEnumType()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
              <edmx:DataServices>
                <Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
                  <EnumType Name="Color" IsFlags="true">
                    <Member Name="Red" Value="1" />
                    <Member Name="Green" Value="2" />
                    <Member Name="Blue" Value="4" />
                  </EnumType>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """;

        var doc = CsdlXmlReader.Read(xml);
        var schema = doc.Edmx!.Schemas[0];
        var enumElem = schema.Elements.OfType<EnumType>().First();

        Assert.Equal("Color", enumElem.Name);
        Assert.True(enumElem.IsFlags);
        Assert.Equal(3, enumElem.Members.Count);
        Assert.Equal("Red", enumElem.Members[0].Name);
        Assert.Equal(1, enumElem.Members[0].Value);
    }

    [Fact]
    public void ReadsFunctionAndAction()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
              <edmx:DataServices>
                <Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
                  <Function Name="GetTopProducts" IsBound="true" IsComposable="false">
                    <Parameter Name="count" Type="Edm.Int32" />
                    <ReturnType Type="Collection(Test.Product)" />
                  </Function>
                  <Action Name="ResetData" IsBound="false">
                    <ReturnType Type="Edm.Boolean" />
                  </Action>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """;

        var doc = CsdlXmlReader.Read(xml);
        var schema = doc.Edmx!.Schemas[0];

        var func = schema.Elements.OfType<Function>().First();
        Assert.Equal("GetTopProducts", func.Name);
        Assert.True(func.IsBound);
        Assert.False(func.IsComposable);
        Assert.Single(func.Parameters);
        Assert.Equal("count", func.Parameters[0].Name);
        Assert.NotNull(func.ReturnType);
        Assert.True(func.ReturnType!.IsCollection);
        Assert.Equal("Test.Product", func.ReturnType.TypeName);

        var action = schema.Elements.OfType<Action>().First();
        Assert.Equal("ResetData", action.Name);
        Assert.False(action.IsBound);
        Assert.NotNull(action.ReturnType);
    }
}

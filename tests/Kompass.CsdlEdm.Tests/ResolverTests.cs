namespace Kompass.CsdlEdm.Tests;

using Kompass.CsdlEdm;
using Kompass.CsdlEdm.Edm;
using Xunit;

public class ResolverTests
{
    private static string LoadFixture(string name)
    {
        var path = Path.Combine("Fixtures", name);
        return File.ReadAllText(path);
    }

    [Fact]
    public void ResolvesRoomsFixture_EntityTypes()
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var model = Resolver.ResolveDocument(doc);

        Assert.Single(model.Schemas);
        var schema = model.Schemas[0];
        Assert.Equal("BuildingManagement", schema.Namespace);

        var entityTypes = schema.Elements
            .OfType<SchemaElement.EntityTypeElement>()
            .Select(e => e.EntityType)
            .ToList();

        Assert.Equal(3, entityTypes.Count);
        Assert.Contains(entityTypes, et => et.Name == "Room");
        Assert.Contains(entityTypes, et => et.Name == "Printer");
        Assert.Contains(entityTypes, et => et.Name == "Phone");
    }

    [Theory]
    [InlineData("Room", 1)]
    [InlineData("Printer", 1)]
    [InlineData("Phone", 1)]
    public void ResolvesRoomsFixture_Keys(string typeName, int expectedKeyCount)
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var model = Resolver.ResolveDocument(doc);
        var schema = model.Schemas[0];

        var entityType = schema.Elements
            .OfType<SchemaElement.EntityTypeElement>()
            .Select(e => e.EntityType)
            .First(et => et.Name == typeName);

        Assert.Equal(expectedKeyCount, entityType.Keys.Count);
        var firstKey = entityType.Keys[0];
        Assert.Single(firstKey);
        Assert.IsType<KeyPathSegment.PropertySegment>(firstKey[0]);
        Assert.Equal("Id", firstKey[0].DisplayName);
    }

    [Fact]
    public void ResolvesRoomsFixture_NavigationProperties()
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var model = Resolver.ResolveDocument(doc);
        var schema = model.Schemas[0];

        var room = schema.Elements
            .OfType<SchemaElement.EntityTypeElement>()
            .Select(e => e.EntityType)
            .First(et => et.Name == "Room");

        Assert.Equal(2, room.NavigationProperties.Count);

        var printers = room.NavigationProperties.First(n => n.Name == "Printers");
        Assert.True(printers.IsCollection);
        Assert.True(printers.ContainsTarget);
        Assert.Equal("Printer", printers.Target.Name);

        var phones = room.NavigationProperties.First(n => n.Name == "Phones");
        Assert.True(phones.IsCollection);
        Assert.True(phones.ContainsTarget);
        Assert.Equal("Phone", phones.Target.Name);
    }

    [Fact]
    public void ResolvesRoomsFixture_EntityContainer()
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var model = Resolver.ResolveDocument(doc);
        var schema = model.Schemas[0];

        Assert.NotNull(schema.EntityContainer);
        Assert.Equal("Container", schema.EntityContainer!.Name);
        Assert.Single(schema.EntityContainer.Elements);

        var esElem = schema.EntityContainer.Elements[0] as EntityContainerElement.EntitySetElement;
        Assert.NotNull(esElem);
        Assert.Equal("Rooms", esElem!.EntitySet.Name);
        Assert.Equal("Room", esElem.EntitySet.Target.Name);
    }

    [Fact]
    public void ResolvesPropertyTypes()
    {
        var xml = LoadFixture("rooms.csdl.xml");
        var doc = CsdlXmlReader.Read(xml);
        var model = Resolver.ResolveDocument(doc);
        var schema = model.Schemas[0];

        var room = schema.Elements
            .OfType<SchemaElement.EntityTypeElement>()
            .Select(e => e.EntityType)
            .First(et => et.Name == "Room");

        var idProp = room.Properties.First(p => p.Name == "Id");
        Assert.IsType<ResolvedType.Primitive>(idProp.Type);
        Assert.Equal(PrimitiveType.String, ((ResolvedType.Primitive)idProp.Type).Type);
    }

    [Fact]
    public void ResolvesInheritance()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
              <edmx:DataServices>
                <Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
                  <EntityType Name="Person">
                    <Key><PropertyRef Name="Id" /></Key>
                    <Property Name="Id" Type="Edm.String" Nullable="false" />
                    <Property Name="Name" Type="Edm.String" />
                  </EntityType>
                  <EntityType Name="Employee" BaseType="Test.Person">
                    <Property Name="Department" Type="Edm.String" />
                  </EntityType>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """;

        var doc = CsdlXmlReader.Read(xml);
        var model = Resolver.ResolveDocument(doc);
        var schema = model.Schemas[0];

        var employee = schema.Elements
            .OfType<SchemaElement.EntityTypeElement>()
            .Select(e => e.EntityType)
            .First(et => et.Name == "Employee");

        Assert.NotNull(employee.BaseType);
        Assert.Equal("Person", employee.BaseType!.Name);

        // Employee inherits keys from Person
        Assert.Single(employee.Keys);
        Assert.Equal("Id", employee.Keys[0][0].DisplayName);

        // Employee has its own property
        Assert.Single(employee.Properties);
        Assert.Equal("Department", employee.Properties[0].Name);
    }

    [Fact]
    public void ThrowsOnUnknownEntityType()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
              <edmx:DataServices>
                <Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
                  <EntityType Name="Room">
                    <Key><PropertyRef Name="Id" /></Key>
                    <Property Name="Id" Type="Edm.String" Nullable="false" />
                    <NavigationProperty Name="Items" Type="Collection(Test.NonExistent)" ContainsTarget="true" />
                  </EntityType>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """;

        var doc = CsdlXmlReader.Read(xml);
        Assert.Throws<UnknownEntityException>(() => Resolver.ResolveDocument(doc));
    }

    [Fact]
    public void ResolvesComplexType()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <edmx:Edmx Version="4.0" xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx">
              <edmx:DataServices>
                <Schema Namespace="Test" xmlns="http://docs.oasis-open.org/odata/ns/edm">
                  <ComplexType Name="Address">
                    <Property Name="Street" Type="Edm.String" />
                    <Property Name="City" Type="Edm.String" />
                    <Property Name="Zip" Type="Edm.String" />
                  </ComplexType>
                  <EntityType Name="Person">
                    <Key><PropertyRef Name="Id" /></Key>
                    <Property Name="Id" Type="Edm.Int32" Nullable="false" />
                    <Property Name="Home" Type="Test.Address" />
                  </EntityType>
                  <EntityContainer Name="Container">
                    <EntitySet Name="People" EntityType="Test.Person" />
                  </EntityContainer>
                </Schema>
              </edmx:DataServices>
            </edmx:Edmx>
            """;

        var doc = CsdlXmlReader.Read(xml);
        var model = Resolver.ResolveDocument(doc);
        var schema = model.Schemas[0];

        var person = schema.Elements
            .OfType<SchemaElement.EntityTypeElement>()
            .Select(e => e.EntityType)
            .First(et => et.Name == "Person");

        var homeProp = person.Properties.First(p => p.Name == "Home");
        Assert.IsType<ResolvedType.Complex>(homeProp.Type);
        var complexType = ((ResolvedType.Complex)homeProp.Type).Type;
        Assert.Equal("Address", complexType.Name);
        Assert.Equal(3, complexType.Properties.Count);
    }
}

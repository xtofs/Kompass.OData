namespace Kompass.CsdlEdm.Benchmarks;

using BenchmarkDotNet.Attributes;
using Kompass.CsdlEdm;
using Kompass.CsdlEdm.Edm;

[MemoryDiagnoser]
[SimpleJob]
public class EdmConsumptionBenchmarks
{
    private DocumentModel _model = null!;
    private Model _schema = null!;

    [GlobalSetup]
    public void Setup()
    {
        var xml = File.ReadAllText(Path.Combine("Fixtures", "large.csdl.xml"));
        var doc = CsdlXmlReader.Read(xml);
        _model = Resolver.ResolveDocument(doc);
        _schema = _model.Schemas[0];
    }

    [Benchmark]
    public EntityType? LookupEntityTypeByName()
    {
        foreach (var element in _schema.Elements)
        {
            if (element is SchemaElement.EntityTypeElement ete && ete.EntityType.Name == "Entity50")
            {
                return ete.EntityType;
            }
        }
        return null;
    }

    [Benchmark]
    public int IterateAllEntityTypesAndProperties()
    {
        var count = 0;
        foreach (var element in _schema.Elements)
        {
            if (element is SchemaElement.EntityTypeElement ete)
            {
                foreach (var prop in ete.EntityType.Properties)
                {
                    count++;
                }
            }
        }
        return count;
    }

    [Benchmark]
    public int EnumerateEntitySets()
    {
        var count = 0;
        if (_schema.EntityContainer is not null)
        {
            foreach (var elem in _schema.EntityContainer.Elements)
            {
                if (elem is EntityContainerElement.EntitySetElement)
                {
                    count++;
                }
            }
        }
        return count;
    }

    [Benchmark]
    public int ResolveKeyProperties()
    {
        var count = 0;
        foreach (var element in _schema.Elements)
        {
            if (element is SchemaElement.EntityTypeElement ete)
            {
                count += ete.EntityType.Keys.Count;
            }
        }
        return count;
    }

    [Benchmark]
    public int WalkAllSchemaElements()
    {
        var count = 0;
        foreach (var element in _schema.Elements)
        {
            count++;
        }
        return count;
    }
}

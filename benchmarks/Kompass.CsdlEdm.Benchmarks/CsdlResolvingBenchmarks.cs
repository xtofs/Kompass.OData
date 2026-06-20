namespace Kompass.CsdlEdm.Benchmarks;

using BenchmarkDotNet.Attributes;
using Kompass.CsdlEdm;
using Kompass.CsdlEdm.Csdl;
using Kompass.CsdlEdm.Edm;

[MemoryDiagnoser]
[SimpleJob]
public class CsdlResolvingBenchmarks
{
    private CsdlDocument _smallDoc = null!;
    private CsdlDocument _mediumDoc = null!;
    private CsdlDocument _largeDoc = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallDoc = CsdlXmlReader.Read(File.ReadAllText(Path.Combine("Fixtures", "small.csdl.xml")));
        _mediumDoc = CsdlXmlReader.Read(File.ReadAllText(Path.Combine("Fixtures", "medium.csdl.xml")));
        _largeDoc = CsdlXmlReader.Read(File.ReadAllText(Path.Combine("Fixtures", "large.csdl.xml")));
    }

    [Benchmark]
    public DocumentModel Resolve_Small() => Resolver.ResolveDocument(_smallDoc);

    [Benchmark]
    public DocumentModel Resolve_Medium() => Resolver.ResolveDocument(_mediumDoc);

    [Benchmark]
    public DocumentModel Resolve_Large() => Resolver.ResolveDocument(_largeDoc);
}

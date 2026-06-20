namespace Kompass.CsdlEdm.Benchmarks;

using BenchmarkDotNet.Attributes;
using Kompass.CsdlEdm;

[MemoryDiagnoser]
[SimpleJob]
public class CsdlReadingBenchmarks
{
    private string _smallXml = null!;
    private string _mediumXml = null!;
    private string _largeXml = null!;

    [GlobalSetup]
    public void Setup()
    {
        _smallXml = File.ReadAllText(Path.Combine("Fixtures", "small.csdl.xml"));
        _mediumXml = File.ReadAllText(Path.Combine("Fixtures", "medium.csdl.xml"));
        _largeXml = File.ReadAllText(Path.Combine("Fixtures", "large.csdl.xml"));
    }

    [Benchmark]
    public Csdl.CsdlDocument ReadXml_Small() => CsdlXmlReader.Read(_smallXml);

    [Benchmark]
    public Csdl.CsdlDocument ReadXml_Medium() => CsdlXmlReader.Read(_mediumXml);

    [Benchmark]
    public Csdl.CsdlDocument ReadXml_Large() => CsdlXmlReader.Read(_largeXml);
}

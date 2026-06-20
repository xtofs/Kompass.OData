# Kompass.OData

A modular OData library suite for .NET. Each assembly is independently usable and focused on a single responsibility.

## Assemblies

| Assembly | Responsibility |
| --- | --- |
| **Kompass.CsdlEdm** | CSDL XML/JSON reader, syntactic model, resolver, resolved EDM model, and validator. |
| **Kompass.OData.Url** | OData URL and query-string parser (`$filter`, `$select`, `$expand`, `$orderby`, `$top`/`$skip`). |
| **Kompass.OData.Routing** | ASP.NET middleware that rewrites OData subsegment-key URLs into segment form for standard routing. |
| **Kompass.OData.Service** | Service builder, handler context types, and endpoint registration via EDM constructs (ASP.NET Minimal API). |
| **Kompass.OData.ResponseShaping** | OData-aware JSON response envelope construction, `$select` projection, and system annotations (`@odata.context`, `@odata.count`, `@odata.nextLink`). |
| **Kompass.OData** | Umbrella meta-package referencing all sub-assemblies. |

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/) or later

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run Benchmarks

```bash
dotnet run --project benchmarks/Kompass.CsdlEdm.Benchmarks -c Release
```

## Solution Layout

```
Kompass.OData/
├── src/
│   ├── Kompass.CsdlEdm/
│   ├── Kompass.OData/
│   ├── Kompass.OData.ResponseShaping/
│   ├── Kompass.OData.Routing/
│   ├── Kompass.OData.Service/
│   └── Kompass.OData.Url/
├── tests/
│   ├── Kompass.CsdlEdm.Tests/
│   ├── Kompass.OData.Routing.Tests/
│   ├── Kompass.OData.Service.Tests/
│   └── Kompass.OData.Url.Tests/
├── benchmarks/
│   └── Kompass.CsdlEdm.Benchmarks/
└── samples/
    ├── Kompass.OData.Samples.Graph/
    └── Kompass.OData.Samples.Rooms/
```

## License

This project is licensed under the [GNU Lesser General Public License v3.0](LICENSE).

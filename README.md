# Kompass.OData

A modular OData library suite for .NET, built around a **schema-first** philosophy.
You define your data model in CSDL; the framework derives handler registrations from that schema and validates them for completeness—flagging entity sets or contained navigations that lack handlers before the service starts.
Internally, the library cleanly separates a **syntactic CSDL tree** (where type references are plain strings) from a **semantic EDM model** (where every reference is resolved to a direct object link).
A two-pass resolver bridges the two: the first pass registers all named elements, the second resolves every cross-reference—base types, navigation targets, partners, and bindings—into typed object pointers.
Each assembly is independently usable and focused on a single responsibility.

## Quick Example

```csharp
var csdl = File.ReadAllText("rooms.csdl.xml");

var service = ODataServiceBuilder.FromCsdl<RoomsRepository>(csdl)
    .EntitySet("Rooms", es => es
        .OnList(ListRooms)                   // GET /Rooms
        .OnGet(GetRoom)                      // GET /Rooms/{id}
        .ContainedCollection("Printers", nav => nav
            .OnList(ListPrinters)            // GET /Rooms/{id}/Printers
            .OnGet(GetPrinter)));            // GET /Rooms/{id}/Printers/{id}

service.MapODataEndpoints(app);
```

Handlers receive a typed context and the `TState` dependency from DI:

```csharp
static async Task<IResult> ListRooms(CollectionContext ctx, RoomsRepository repo)
{
    var (items, count) = await repo.GetRoomsAsync(
        skip: (int?)ctx.Query.Page.Skip,
        top:  (int?)ctx.Query.Page.Top);
    return ctx.Ok(items.Cast<object>(), ctx.Query.Count == true ? count : null);
}
```

See [`samples/`](samples/) for complete runnable examples.

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

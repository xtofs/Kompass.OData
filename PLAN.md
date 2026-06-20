# Kompass.OData — Full Library Suite Plan

## Goal

Port the architecture from [odata-rs](https://github.com/xtofs/odata-rs) to C#, producing a set of independent assemblies that mirror the Rust crate decomposition. Additionally, add **benchmarks** (which don't exist in odata-rs) and **demo applications**.

## Assembly Mapping (Rust crates → C# projects)

| Rust crate | C# assembly | Responsibility |
| --- | --- | --- |
| `csdl-edm` | **Kompass.CsdlEdm** | CSDL XML/JSON reader, syntactic model (`Csdl.*`), resolver, resolved EDM model (`Edm.*`), validator. No HTTP, no async. |
| `odata-rs-url` | **Kompass.OData.Url** | OData URL + query-string parser → `ODataQuery`, `QueryOptions`. No HTTP, no schema knowledge. |
| `odata-rs-routing` | **Kompass.OData.Routing** | ASP.NET middleware that rewrites OData subsegment-key URLs into segment form for standard routing. Exports `OriginalODataUri`. |
| `odata-rs-service` | **Kompass.OData.Service** | Service builder, handler context types, endpoint registration via EDM constructs. Integrates with ASP.NET Minimal API. |
| *(new — not in odata-rs)* | **Kompass.OData.ResponseShaping** | OData-aware JSON response envelope construction, `$select` projection, `@odata.context`/`@odata.count`/`@odata.nextLink` annotations. |
| umbrella re-export | **Kompass.OData** | Meta-package / umbrella that references all sub-assemblies. |

## Solution Structure

```javascript
Kompass.OData/
├── Kompass.OData.sln
├── .editorconfig
│
├── src/
│   ├── Kompass.CsdlEdm/                        # CSDL parsing + EDM resolution
│   │   ├── Csdl/                             # Syntactic model types
│   │   │   ├── CsdlDocument.cs               # Root: Edmx, Schema, SchemaElement
│   │   │   ├── EntityType.cs, ComplexType.cs  # Type definitions
│   │   │   ├── NavigationProperty.cs          # Nav props with containment
│   │   │   ├── Property.cs                   # Structural properties + facets
│   │   │   ├── EnumType.cs                   # Enum types + members
│   │   │   ├── EntityContainer.cs            # Container, EntitySet, Singleton
│   │   │   └── Operation.cs                  # Function, Action, Parameter, ReturnType
│   │   ├── Edm/                              # Resolved semantic model
│   │   │   ├── Model.cs                      # DocumentModel, Model, SchemaElement
│   │   │   ├── EntityType.cs                 # Resolved entity type with keys
│   │   │   ├── NavigationProperty.cs         # Resolved nav with target reference
│   │   │   ├── Property.cs                   # Resolved property with ResolvedType
│   │   │   ├── EntityContainer.cs            # Resolved container elements
│   │   │   ├── PrimitiveType.cs              # Edm.String, Edm.Int32, etc.
│   │   │   └── PathSegment.cs                # BindingPathSegment, KeyPathSegment
│   │   ├── CsdlXmlReader.cs                  # XML → CsdlDocument
│   │   ├── CsdlJsonReader.cs                 # JSON → CsdlDocument
│   │   ├── Resolver.cs                       # Pass 1→2: syntactic → semantic
│   │   └── Validator.cs                      # Semantic validation
│   │
│   ├── Kompass.OData.Url/                       # URL + query-string parser
│   │   ├── ODataQuery.cs                     # Full parsed URL
│   │   ├── QueryOptions.cs                   # System query options subset
│   │   ├── FilterExpression.cs               # $filter AST
│   │   ├── SelectClause.cs                   # $select
│   │   ├── ExpandClause.cs                   # $expand
│   │   ├── OrderByClause.cs                  # $orderby
│   │   ├── Page.cs                           # $top/$skip
│   │   └── ParseError.cs                     # Error types
│   │
│   ├── Kompass.OData.Routing/                   # OData URL rewriting middleware
│   │   ├── ODataPathRewriteMiddleware.cs     # Rewrites /ES('key') → /ES/__key__/key
│   │   ├── OriginalODataUri.cs               # Stashes pre-rewrite URI
│   │   └── ODataRoutingExtensions.cs         # IApplicationBuilder.UseODataRewrite()
│   │
│   ├── Kompass.OData.Service/                   # Service composition + builder
│   │   ├── ODataServiceBuilder.cs            # Builder: registers by EDM constructs
│   │   ├── EntitySetConfig.cs                # Per-entity-set handler config
│   │   ├── ContainedNavConfig.cs             # Per-contained-nav config
│   │   ├── Contexts/                         # Handler context types
│   │   │   ├── CollectionContext.cs
│   │   │   ├── EntityContext.cs
│   │   │   ├── ContainedCollectionContext.cs
│   │   │   └── ContainedEntityContext.cs
│   │   └── SchemaView.cs                     # Internal EDM→route projection
│   │
│   ├── Kompass.OData.ResponseShaping/           # OData JSON envelope + projection
│   │   ├── ODataResponseBuilder.cs           # Builds {"value":[...]} envelopes
│   │   ├── SelectProjector.cs                # $select property filtering
│   │   └── AnnotationWriter.cs               # @odata.context, @odata.count, etc.
│   │
│   └── Kompass.OData/                           # Umbrella package
│       └── Kompass.OData.csproj                 # References all sub-assemblies
│
├── tests/
│   ├── Kompass.CsdlEdm.Tests/                  # CSDL/EDM unit tests
│   │   ├── CsdlXmlReaderTests.cs            # XML parsing tests
│   │   ├── CsdlJsonReaderTests.cs           # JSON parsing tests
│   │   ├── ResolverTests.cs                 # Resolution: key paths, nav props, etc.
│   │   ├── ValidatorTests.cs                # Semantic validation tests
│   │   └── Fixtures/                        # Shared CSDL fixtures
│   ├── Kompass.OData.Url.Tests/                # URL parser tests
│   │   ├── ODataQueryTests.cs               # Full URL parsing
│   │   ├── FilterExpressionTests.cs         # $filter AST parsing
│   │   └── QueryOptionsTests.cs             # System query options
│   ├── Kompass.OData.Routing.Tests/            # Routing middleware tests
│   │   └── PathRewriteTests.cs              # Rewrite correctness
│   └── Kompass.OData.Service.Tests/            # Service builder + context tests
│       ├── ServiceBuilderTests.cs           # Registration + validation
│       └── ContextExtractionTests.cs        # Context creation from requests
│
├── benchmarks/
│   └── Kompass.CsdlEdm.Benchmarks/            # BenchmarkDotNet
│       ├── Program.cs
│       ├── Fixtures/                        # Small / Medium / Large × XML + JSON
│       ├── CsdlReadingBenchmarks.cs         # XML/JSON → CsdlDocument
│       ├── CsdlResolvingBenchmarks.cs       # CsdlDocument → Model
│       └── EdmConsumptionBenchmarks.cs      # Model traversal patterns
│
└── samples/
    ├── Rooms/            # Small demo (mirrors odata-rs rooms)
    │   ├── Program.cs
    │   ├── rooms.csdl.xml
    │   └── Handlers/
    └── Graph/            # Larger demo (Graph-like)
        ├── Program.cs
        ├── graph.csdl.xml
        └── Handlers/
```

---

## Phase 1: Kompass.CsdlEdm (Foundation)

### 1A. Syntactic Model (`Csdl/` namespace)

Mirrors `csdl.rs`. Pure data types — no parsing logic here.

**Key types:**

- `CsdlDocument` → root with `Edmx`
- `Edmx` → version, references, schemas
- `Schema` → namespace, alias, elements list
- `SchemaElement` (abstract base or discriminated union via inheritance) → `EntityType`, `ComplexType`, `EnumType`, `TypeDefinition`, `Term`, `Function`, `Action`, `EntityContainer`
- `EntityType` → name, baseType, isAbstract, isOpen, hasStream, key, properties, navigationProperties
- `Property` → name, typeName, isCollection, nullable, facets (MaxLength, Precision, Scale, SRID)
- `NavigationProperty` → name, typeName, isCollection, nullable, partner, containsTarget, onDelete, referentialConstraints
- `EntityContainer` → name, extends, entitySets, singletons, functionImports, actionImports
- `EntitySet` → name, entityType, navigationPropertyBindings
- `Function` / `Action` → name, isBound, parameters, returnType

### 1B. CSDL XML Reader

Mirrors `csdl_xml_reader.rs`. Streaming `XmlReader`-based parser that produces a `CsdlDocument`.

- Stack-machine approach: push element, populate fields, pop into parent
- All type references remain as strings (no resolution)
- Handles aliases as-written

### 1C. CSDL JSON Reader

Mirrors `csdl_json_reader.rs`. `System.Text.Json`-based parser.

- JSON CSDL uses the OASIS JSON format (property-keyed schemas, `$Kind` discriminators)
- Produces the same `CsdlDocument` as the XML reader

### 1D. Resolved EDM Model (`Edm/` namespace)

Mirrors `edm.rs`. The semantic, fully-resolved model.

**Key types:**

- `DocumentModel` → version, references, schemas (list of `Model`)
- `Model` → namespace, alias, elements, entityContainer
- `EntityType` → name, isAbstract, keys (resolved `Property` references), properties, navigationProperties, baseType reference
- `Property` → name, resolvedType (`ResolvedType`), isCollection, nullable
- `NavigationProperty` → name, target (`EntityType` reference), isCollection, partner, containsTarget
- `ResolvedType` (enum-like) → Primitive, Enum, Complex, TypeDefinition
- `PrimitiveType` (enum) → Binary, Boolean, Byte, Date, DateTimeOffset, Decimal, Double, Duration, Guid, Int16, Int32, Int64, SByte, Single, String, TimeOfDay
- `EntityContainer` → name, entitySets, singletons, functionImports, actionImports
- `EntitySet` → name, entityType (reference), navigationPropertyBindings (resolved paths)

**C# vs Rust difference:** No `Arc<T>`/`Weak<T>`/`OnceLock` needed. C# GC handles reference cycles natively. Entity types hold direct object references to their navigation targets.

### 1E. Resolver

Mirrors `resolver.rs`. Two-pass resolution:

1. Register all schemas, assign every named element into lookup dictionaries
2. Resolve all string references into object references: type names → `EntityType`/`ComplexType`/`EnumType`, key paths → `Property` chains, nav-prop targets → `EntityType`, partner paths, navigation property bindings

**Error type:** `ResolveError` with cases for `UnknownType`, `UnknownEntity`, `DuplicateName`, `MissingTypeName`, `UnknownPropertyPath`, `UnsupportedCsdlFeature`.

### 1F. Validator

Mirrors `validator.rs`. Post-resolution semantic checks:

- Key properties must be scalar primitives or simple complex types
- Navigation property targets must exist
- Containment constraints
- Non-scalar key property warnings

---

## Phase 2: Kompass.OData.Url (URL Parser)

Mirrors `odata-rs-url`. Pure parser with no HTTP or schema dependency.

### 2A. ODataQuery + QueryOptions

**`ODataQuery`** — full parsed URL:

- `ResourcePath` (segments), path markers (`$count`, `$ref`, `$value`, `$each`)
- System query options: `Select`, `Filter`, `Expand`, `OrderBy`, `Page` (top/skip), `Count`
- Custom options (non-`$`-prefixed)
- Fragment

**`QueryOptions`** — the subset handlers actually need (no resource path).

### 2B. Filter Expression AST

- `FilterExpression` with `FilterExpressionKind`: Literal, Member, FunctionCall, Unary, Binary
- `FilterLiteral`: Null, Boolean, Number, String
- `FilterBinaryOperator`: Or, And, Eq, Ne, Gt, Ge, Lt, Le, Add, Sub, Mul, Div, Mod
- `FilterUnaryOperator`: Not, Negate
- `FilterFunctionCall`: name + arguments
- Span tracking for error reporting
- Display/ToString roundtrip with correct precedence and parenthesization

### 2C. Parse method

`ODataQuery.Parse(string)` → `ODataQuery` or `ParseError`
`QueryOptions.Parse(string)` → `QueryOptions` or `ParseError`

---

## Phase 3: Kompass.OData.Routing (URL Rewriting Middleware)

Mirrors `odata-rs-routing`. ASP.NET middleware.

### 3A. Path Rewrite Middleware

- Rewrites `/Rooms('oak-204')/Printers('hp-42')` → `/Rooms/__key__/oak-204/Printers/__key__/hp-42`
- Strips single-quote delimiters from string keys
- Sentinel `__key__` never collides with valid OData identifiers
- Stores original URI in `HttpContext.Items` as `OriginalODataUri`

### 3B. Extension Methods

- `IApplicationBuilder.UseODataPathRewrite()` — registers the middleware
- `HttpContext.GetOriginalODataUri()` — retrieves pre-rewrite URI

---

## Phase 4: Kompass.OData.Service (Service Composition)

Mirrors `odata-rs-service`. Uses **ASP.NET Minimal API** instead of Axum.

### 4A. Handler Context Types

Four context structs matching URL shape:

- `CollectionContext` → entitySet, query, body
- `EntityContext` → entitySet, key, query, body
- `ContainedCollectionContext` → entitySet, parentKey, navProp, query, body
- `ContainedEntityContext` → entitySet, parentKey, navProp, key, query, body

### 4B. Service Builder (EDM-construct-driven registration)

```csharp
var service = ODataServiceBuilder.New(model)
    .WithState(serviceProvider)  // or explicit state
    .EntitySet("Rooms", es => es
        .OnList(ListRooms)
        .OnGet(GetRoom)
        .OnCreate(CreateRoom)
        .OnDelete(DeleteRoom)
        .ContainedCollection("Printers", nav => nav
            .OnList(ListPrinters)
            .OnGet(GetPrinter)))
    .Build(app);  // maps to Minimal API endpoints
```

**Key design points:**

- Builder accepts EDM entity-set names and navigation-property names, NOT URL patterns
- Builder validates registrations against the resolved EDM model at build time:
    - Entity set must exist in schema
    - Contained nav props must have `ContainsTarget=true` in the model
    - Unregistered entity sets produce warnings
- `Build()` maps EDM registrations to ASP.NET Minimal API `MapGet`/`MapPost`/`MapPatch`/`MapDelete` calls
- Dual route registration (segment-style + rewrite-style) for OData URL compatibility

### 4C. SchemaView (Internal)

Projects the resolved EDM `Model` into a router-oriented working set: entity set names → entity type info → contained nav props. Used internally by the builder; not public API.

---

## Phase 5: Kompass.OData.ResponseShaping (New — not in odata-rs)

### 5A. ODataResponseBuilder

Constructs OData-compliant JSON envelopes:

- `{"value": [...]}` for collections
- `@odata.context` annotation
- `@odata.count` when `$count=true`
- `@odata.nextLink` for server-driven paging

### 5B. SelectProjector

Applies `$select` to response output (not to the data query):

- Given a set of row objects and a `SelectClause`, emits JSON with only the selected properties
- Always preserves key properties and OData annotations

### 5C. AnnotationWriter

Emits `@odata.id`, `@odata.editLink`, `@odata.type` per entity using the original OData URI (from `OriginalODataUri`).

---

## Phase 6: Tests

### 6A. Kompass.CsdlEdm.Tests

Mirrors test coverage from odata-rs `crates/csdl-edm/tests/`:

- **XML reader**: parse small/medium fixtures, verify all types/properties/nav-props round-trip
- **JSON reader**: same fixtures in JSON format, verify identical `CsdlDocument` output
- **Resolver**: key path resolution (simple, through complex types), nav-prop binding resolution, partner resolution, entity-set-path resolution, import target resolution
- **Validator**: non-scalar key detection, unresolved references flagged, containment constraints
- **Format equivalence**: XML and JSON readers produce semantically identical models

### 6B. Kompass.OData.Url.Tests

Mirrors `crates/url/src/tests.rs`:

- Full URL with all query options
- Path markers as flags ($count, $ref, $value, $each)
- Invalid URL rejection
- Invalid boolean / duplicate option rejection
- Filter precedence parsing
- Filter function calls and member paths
- Filter span tracking
- Display roundtrip (precedence preservation, single-quote escaping)

### 6C. Kompass.OData.Routing.Tests

Mirrors 12 test cases from `crates/routing/src/lib.rs`:

- Plain segment unchanged, collection unchanged
- Single key, integer key, nested contained nav
- Key then plain nav, path marker after key/collection
- Root path, empty key, quoted key with escaped quote, deeply nested

### 6D. Kompass.OData.Service.Tests

- Builder validates entity set exists in schema
- Builder validates contained nav has `ContainsTarget=true`
- Builder warns on unregistered entity sets
- Context extraction from HTTP requests
- Dual route registration produces correct endpoints

---

## Phase 7: Benchmarks (New — not in odata-rs)

BenchmarkDotNet project. Three fixture sizes: Small (\~3 types), Medium (\~20 types), Large (100+ types). Each in XML + JSON.

### 7A. CsdlReadingBenchmarks

Parameterized by size × format. `[GlobalSetup]` pre-loads file into string.

- `ReadCsdlFromXml` — parse XML string → `CsdlDocument`
- `ReadCsdlFromJson` — parse JSON string → `CsdlDocument`

### 7B. CsdlResolvingBenchmarks

Parameterized by size. `[GlobalSetup]` pre-parses CSDL.

- `ResolveCsdlDocument` — `CsdlDocument` → resolved `Model`

### 7C. EdmConsumptionBenchmarks

Pre-loaded model. Benchmarks common patterns:

- `LookupEntityTypeByName` — find entity type by qualified name
- `IterateAllEntityTypesAndProperties` — walk all types + properties
- `NavigateToRelatedEntityTypes` — follow nav props to targets
- `EnumerateEntitySets` — list entity sets with target types
- `ResolveKeyProperties` — resolve key property references
- `WalkInheritanceChain` — traverse base type chains
- `LookupNavigationPropertyBindings` — resolve nav-prop bindings

---

## Phase 8: Demo Applications

### 8A. Rooms Sample (mirrors odata-rs `examples/rooms`)

Small service: `Room` → contained `Printer`, `Phone`. SQLite backend. Shows:

- CSDL loading, builder registration, handler implementation
- `$top`, `$skip`, `$orderby`, `$select` support

### 8B. Graph-like Sample (larger, new)

\~20+ entity types (User, Group, Message, Calendar, Event, Drive, DriveItem, etc.). Shows:

- Inheritance, complex types, enums
- Deeper containment chains
- Multiple entity sets

---

## Rust → C# Translation Notes

| Rust pattern | C# equivalent | Notes |
| --- | --- | --- |
| `Arc<T>` + `Weak<T>` + `OnceLock` | Plain object references | GC handles cycles natively |
| Feature-gated modules | Separate assemblies | Clean dependency boundaries |
| `serde` JSON | `System.Text.Json` | Native, fast |
| `quick-xml` streaming | `System.Xml.XmlReader` | Streaming parity |
| Trait `Display` | `ToString()` / `IFormattable` | Standard .NET |
| Axum `Router` + `middleware::from_fn` | ASP.NET Minimal API + middleware pipeline | Builder maps EDM constructs → endpoints |
| `Router::with_state(S)` | DI container or explicit state on builder | Standard ASP.NET pattern |
| Rust enums (algebraic) | Abstract base class + sealed subclasses, or C# discriminated unions | Use sealed hierarchy for `SchemaElement`, `FilterExpressionKind`, etc. |
| `Result<T, E>` pervasive | Exceptions for truly-exceptional + `Result<T>` types where error flow is expected | Use custom `Result<T>` or exception depending on context |

---

## Implementation Status (as of 2026-06-19)

**All 8 phases are implemented. 63 tests pass. Both sample apps work E2E.**

### Key Architectural Decisions Made

#### Generic State Pattern (like Axum `with_state`)
- `ODataServiceBuilder` = non-generic static factory with `FromCsdl()` (defaults `TState=IServiceProvider`) and `FromCsdl<TState>()`
- `ODataServiceBuilder<TState> where TState : notnull` = the actual generic builder
- State resolved at request time via `services.GetRequiredService<TState>()`
- Handler signatures: `Func<CollectionContext, TState, Task<IResult>>`
- `IServiceProvider` IS resolvable from ASP.NET Core DI (returns scoped provider) — backward compat

#### Context Hierarchy
```
ODataContext (abstract base)
├── CollectionContext          — Ok(items, count?, nextLink?), Created(entity)
├── EntityContext              — Key, Ok(entity), NoContent()
├── ContainedCollectionContext — ParentKey, NavProp
└── ContainedEntityContext     — ParentKey, NavProp, Key
```
- **Shared base** carries: `EntitySet`, `Body` (Stream), `Query` (lazy-parsed), `ContextUrl`, `NotFound()`, `BadRequest()`
- **Lazy Query**: raw query string stored; parsed to `QueryOptions` on first `.Query` access via `??=`
- **Stream Body**: raw `HttpContext.Request.Body` (or `Stream.Null` for DELETE). Convenience: `ReadBodyAsStringAsync()`, `ReadBodyAsJsonAsync<T>()`

#### Route Registration & Printing
- Dual route registration: `/EntitySet/__key__/{id}` (rewrite sentinel) + `/EntitySet/{id}` (key-as-segment)
- `ODataPathRewriter.FormatAsODataPath()` reverses sentinel patterns back to OData form
- `app.GetODataRoutes()` / `app.PrintODataRoutes()` — queries ASP.NET `EndpointDataSource`, filters out sentinels, deduplicates
- Must be called after `Build()` — use `app.Lifetime.ApplicationStarted.Register()`

#### Rooms Sample Architecture
- `RoomsRepository` — async in-memory repository (`GetRoomsAsync`, `GetRoomAsync`, etc.)
- Registered as singleton: `builder.Services.AddSingleton<RoomsRepository>()`
- Builder: `ODataServiceBuilder.FromCsdl<RoomsRepository>(csdlXml)`
- Handlers: `async Task<IResult> ListRooms(CollectionContext ctx, RoomsRepository repo)`

### Test Counts
| Project | Tests |
| --- | --- |
| Kompass.CsdlEdm.Tests | 22 |
| Kompass.OData.Url.Tests | 20 |
| Kompass.OData.Routing.Tests | 14 |
| Kompass.OData.Service.Tests | 7 |
| **Total** | **63** |

### Build Environment
- Windows, .NET SDK 10.0.301, targeting net9.0
- Solution uses `.slnx` format: `Kompass.OData.slnx`
- Benchmarks use BenchmarkDotNet

### C# Coding Style (from instructions)
- `var` everywhere, file-scoped namespaces, usings inside namespace, always braces
- No shortened `new()`, prefer modern collection initializers
- `Theory`+`InlineData` for tests, `MemberData` for complex data

### Rename History
- Originally named `Nabe.OData`, renamed to `Kompass.OData` on 2026-06-19
- 67 source files updated, 14 files renamed, 13 directories renamed
- Root folder rename (`Nabe.OData` → `Kompass.OData`) done manually after session
namespace Kompass.OData.Service;

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Kompass.CsdlEdm.Edm;
using Kompass.OData.Routing;
using Kompass.OData.Service.Contexts;

/// <summary>
/// Static factory methods for creating <see cref="ODataServiceBuilder{TState}"/> instances.
/// </summary>
public static class ODataServiceBuilder
{
    /// <summary>
    /// Create a builder with typed state from a resolved EDM model.
    /// At Build() time, <typeparamref name="TState"/> is resolved from the DI container.
    /// </summary>
    public static ODataServiceBuilder<TState> New<TState>(Model model) where TState : notnull
    {
        return ODataServiceBuilder<TState>.CreateFromModel(model);
    }

    /// <summary>
    /// Create a builder with typed state from a CSDL XML string (parse + resolve in one step).
    /// </summary>
    public static ODataServiceBuilder<TState> FromCsdl<TState>(string csdlXml) where TState : notnull
    {
        return ODataServiceBuilder<TState>.CreateFromCsdl(csdlXml);
    }

    /// <summary>
    /// Create a builder from a resolved EDM model.
    /// State defaults to <see cref="IServiceProvider"/> for backward compatibility.
    /// </summary>
    public static ODataServiceBuilder<IServiceProvider> New(Model model)
    {
        return New<IServiceProvider>(model);
    }

    /// <summary>
    /// Create a builder from a CSDL XML string.
    /// State defaults to <see cref="IServiceProvider"/> for backward compatibility.
    /// </summary>
    public static ODataServiceBuilder<IServiceProvider> FromCsdl(string csdlXml)
    {
        return FromCsdl<IServiceProvider>(csdlXml);
    }
}

/// <summary>
/// Builder that registers OData entity-set handlers by EDM constructs (not URL patterns)
/// and maps them to ASP.NET Minimal API endpoints.
/// <typeparamref name="TState"/> is the shared state type passed to every handler,
/// resolved from DI at request time (similar to Axum's <c>Router::with_state</c>).
/// </summary>
public sealed class ODataServiceBuilder<TState> where TState : notnull
{
    private readonly SchemaView _schema;
    private readonly Dictionary<string, EntitySetConfig<TState>> _configs = new Dictionary<string, EntitySetConfig<TState>>();
    private readonly List<string> _warnings = new List<string>();

    private ODataServiceBuilder(SchemaView schema)
    {
        _schema = schema;
    }

    internal static ODataServiceBuilder<TState> CreateFromModel(Model model)
    {
        var schema = SchemaView.FromModel(model);
        return new ODataServiceBuilder<TState>(schema);
    }

    internal static ODataServiceBuilder<TState> CreateFromCsdl(string csdlXml)
    {
        var doc = CsdlEdm.CsdlXmlReader.Read(csdlXml);
        var docModel = CsdlEdm.Resolver.ResolveDocument(doc);
        var model = docModel.Schemas[0];
        return CreateFromModel(model);
    }

    /// <summary>
    /// Register handlers for an entity set by name.
    /// </summary>
    public ODataServiceBuilder<TState> EntitySet(string name, Func<EntitySetConfig<TState>, EntitySetConfig<TState>> configure)
    {
        var esView = _schema.FindEntitySet(name);
        if (esView is null)
        {
            throw new InvalidOperationException(
                $"Entity set '{name}' not found in schema. Available: {string.Join(", ", _schema.EntitySets.Select(e => e.Name))}");
        }

        // Validate contained nav props
        var config = configure(new EntitySetConfig<TState>());
        var entityType = _schema.FindEntityType(esView.EntityTypeName);
        if (entityType is not null)
        {
            foreach (var navName in config.ContainedNavs.Keys)
            {
                var found = false;
                foreach (var nav in entityType.ContainedNavigationProperties)
                {
                    if (nav.Name == navName)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new InvalidOperationException(
                        $"Navigation property '{navName}' on entity type '{entityType.Name}' is not a contained navigation property.");
                }
            }
        }

        _configs[name] = config;
        return this;
    }

    /// <summary>
    /// Get warnings about unregistered entity sets or contained navigations.
    /// </summary>
    public IReadOnlyList<string> GetWarnings()
    {
        var warnings = new List<string>();

        foreach (var es in _schema.EntitySets)
        {
            if (!_configs.ContainsKey(es.Name))
            {
                warnings.Add($"Entity set '{es.Name}' has no registered handlers.");
            }
            else
            {
                var entityType = _schema.FindEntityType(es.EntityTypeName);
                if (entityType is not null)
                {
                    var config = _configs[es.Name];
                    foreach (var nav in entityType.ContainedNavigationProperties)
                    {
                        if (!config.ContainedNavs.ContainsKey(nav.Name))
                        {
                            warnings.Add(
                                $"Contained navigation '{nav.Name}' on entity set '{es.Name}' has no registered handlers.");
                        }
                    }
                }
            }
        }

        return warnings;
    }

    /// <summary>
    /// Map all registered entity sets to ASP.NET Minimal API endpoints.
    /// Registers dual routes (standard + rewrite-style) for each operation.
    /// <typeparamref name="TState"/> is resolved from DI on each request.
    /// </summary>
    public void MapODataEndpoints(IEndpointRouteBuilder endpoints)
    {
        foreach (var (entitySetName, config) in _configs)
        {
            var esName = entitySetName;
            var key = ODataPathRewriter.KeySegment;

            // Collection routes: GET /EntitySet and POST /EntitySet
            if (config.ListHandler is not null)
            {
                var handler = config.ListHandler;
                var pattern = $"/{esName}";
                endpoints.MapGet(pattern, async (HttpContext ctx) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}";
                    var context = new CollectionContext(esName, rawQuery, ctx.Request.Body, contextUrl);
                    return await handler(context, state);
                });
            }

            if (config.CreateHandler is not null)
            {
                var handler = config.CreateHandler;
                var pattern = $"/{esName}";
                endpoints.MapPost(pattern, async (HttpContext ctx) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}";
                    var context = new CollectionContext(esName, rawQuery, ctx.Request.Body, contextUrl);
                    return await handler(context, state);
                });
            }

            // Entity routes: dual registration
            // Rewrite-style: /EntitySet/__key__/{id} (for parenthesized-key rewrite)
            // Standard:      /EntitySet/{id}         (for direct segment access)
            if (config.GetHandler is not null)
            {
                var handler = config.GetHandler;
                var sentinelPattern = $"/{esName}/{key}/{{id}}";
                var segmentPattern = $"/{esName}/{{id}}";
                endpoints.MapGet(sentinelPattern, async (HttpContext ctx, string id) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}/$entity";
                    var context = new EntityContext(esName, id, rawQuery, ctx.Request.Body, contextUrl);
                    return await handler(context, state);
                });
                endpoints.MapGet(segmentPattern, async (HttpContext ctx, string id) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}/$entity";
                    var context = new EntityContext(esName, id, rawQuery, ctx.Request.Body, contextUrl);
                    return await handler(context, state);
                });
            }

            if (config.UpdateHandler is not null)
            {
                var handler = config.UpdateHandler;
                var sentinelPattern = $"/{esName}/{key}/{{id}}";
                var segmentPattern = $"/{esName}/{{id}}";
                endpoints.MapPatch(sentinelPattern, async (HttpContext ctx, string id) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}/$entity";
                    var context = new EntityContext(esName, id, rawQuery, ctx.Request.Body, contextUrl);
                    return await handler(context, state);
                });
                endpoints.MapPatch(segmentPattern, async (HttpContext ctx, string id) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}/$entity";
                    var context = new EntityContext(esName, id, rawQuery, ctx.Request.Body, contextUrl);
                    return await handler(context, state);
                });
            }

            if (config.DeleteHandler is not null)
            {
                var handler = config.DeleteHandler;
                var sentinelPattern = $"/{esName}/{key}/{{id}}";
                var segmentPattern = $"/{esName}/{{id}}";
                endpoints.MapDelete(sentinelPattern, async (HttpContext ctx, string id) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}/$entity";
                    var context = new EntityContext(esName, id, rawQuery, Stream.Null, contextUrl);
                    return await handler(context, state);
                });
                endpoints.MapDelete(segmentPattern, async (HttpContext ctx, string id) =>
                {
                    var state = ResolveState(ctx.RequestServices);
                    var rawQuery = ExtractRawQuery(ctx);
                    var contextUrl = $"$metadata#{esName}/$entity";
                    var context = new EntityContext(esName, id, rawQuery, Stream.Null, contextUrl);
                    return await handler(context, state);
                });
            }

            // Contained navigation routes (also dual-registered)
            foreach (var (navName, navConfig) in config.ContainedNavs)
            {
                var nav = navName;

                if (navConfig.ListHandler is not null)
                {
                    var handler = navConfig.ListHandler;

                    async Task<IResult> Handler (HttpContext ctx, string parentId) 
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}";
                        var context = new ContainedCollectionContext(esName, parentId, nav, rawQuery, ctx.Request.Body, contextUrl);
                        return await handler(context, state);
                    }

                    var sentinelPattern = $"/{esName}/{key}/{{parentId}}/{nav}";
                    var segmentPattern = $"/{esName}/{{parentId}}/{nav}";

                    endpoints.MapGet(sentinelPattern, Handler);
                    endpoints.MapGet(segmentPattern, Handler);
                }

                if (navConfig.CreateHandler is not null)
                {
                    var handler = navConfig.CreateHandler;
                    var sentinelPattern = $"/{esName}/{key}/{{parentId}}/{nav}";
                    var segmentPattern = $"/{esName}/{{parentId}}/{nav}";
                    endpoints.MapPost(sentinelPattern, async (HttpContext ctx, string parentId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}";
                        var context = new ContainedCollectionContext(esName, parentId, nav, rawQuery, ctx.Request.Body, contextUrl);
                        return await handler(context, state);
                    });
                    endpoints.MapPost(segmentPattern, async (HttpContext ctx, string parentId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}";
                        var context = new ContainedCollectionContext(esName, parentId, nav, rawQuery, ctx.Request.Body, contextUrl);
                        return await handler(context, state);
                    });
                }

                if (navConfig.GetHandler is not null)
                {
                    var handler = navConfig.GetHandler;
                    var sentinelPattern = $"/{esName}/{key}/{{parentId}}/{nav}/{key}/{{navId}}";
                    var segmentPattern = $"/{esName}/{{parentId}}/{nav}/{{navId}}";
                    endpoints.MapGet(sentinelPattern, async (HttpContext ctx, string parentId, string navId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}/$entity";
                        var context = new ContainedEntityContext(esName, parentId, nav, navId, rawQuery, ctx.Request.Body, contextUrl);
                        return await handler(context, state);
                    });
                    endpoints.MapGet(segmentPattern, async (HttpContext ctx, string parentId, string navId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}/$entity";
                        var context = new ContainedEntityContext(esName, parentId, nav, navId, rawQuery, ctx.Request.Body, contextUrl);
                        return await handler(context, state);
                    });
                }

                if (navConfig.UpdateHandler is not null)
                {
                    var handler = navConfig.UpdateHandler;
                    var sentinelPattern = $"/{esName}/{key}/{{parentId}}/{nav}/{key}/{{navId}}";
                    var segmentPattern = $"/{esName}/{{parentId}}/{nav}/{{navId}}";
                    endpoints.MapPatch(sentinelPattern, async (HttpContext ctx, string parentId, string navId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}/$entity";
                        var context = new ContainedEntityContext(esName, parentId, nav, navId, rawQuery, ctx.Request.Body, contextUrl);
                        return await handler(context, state);
                    });
                    endpoints.MapPatch(segmentPattern, async (HttpContext ctx, string parentId, string navId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}/$entity";
                        var context = new ContainedEntityContext(esName, parentId, nav, navId, rawQuery, ctx.Request.Body, contextUrl);
                        return await handler(context, state);
                    });
                }

                if (navConfig.DeleteHandler is not null)
                {
                    var handler = navConfig.DeleteHandler;
                    var sentinelPattern = $"/{esName}/{key}/{{parentId}}/{nav}/{key}/{{navId}}";
                    var segmentPattern = $"/{esName}/{{parentId}}/{nav}/{{navId}}";
                    endpoints.MapDelete(sentinelPattern, async (HttpContext ctx, string parentId, string navId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}/$entity";
                        var context = new ContainedEntityContext(esName, parentId, nav, navId, rawQuery, Stream.Null, contextUrl);
                        return await handler(context, state);
                    });
                    endpoints.MapDelete(segmentPattern, async (HttpContext ctx, string parentId, string navId) =>
                    {
                        var state = ResolveState(ctx.RequestServices);
                        var rawQuery = ExtractRawQuery(ctx);
                        var contextUrl = $"$metadata#{esName}('{parentId}')/{nav}/$entity";
                        var context = new ContainedEntityContext(esName, parentId, nav, navId, rawQuery, Stream.Null, contextUrl);
                        return await handler(context, state);
                    });
                }
            }
        }
    }

    private static TState ResolveState(IServiceProvider services)
    {
        return services.GetRequiredService<TState>();
    }

    private static string ExtractRawQuery(HttpContext ctx)
    {
        return ctx.Request.QueryString.Value?.TrimStart('?') ?? "";
    }

    /// <summary>
    /// Generate OData service document JSON listing all entity sets.
    /// </summary>
    public IResult GenerateServiceDocument(string baseUrl)
    {
        var entitySets = _schema.EntitySets.Select(es => new
        {
            name = es.Name,
            kind = "EntitySet",
            url = es.Name,
        });

        var doc = new
        {
            value = entitySets,
        };

        return Results.Json(doc);
    }

    public void MapServiceDocumentEndpoint(WebApplication app, string v)
    {
        var doc = this.GenerateServiceDocument("https://localhost:5000");
        
        app.MapGet("/", () => doc);

    }
}

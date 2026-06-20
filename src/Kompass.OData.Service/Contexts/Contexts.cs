namespace Kompass.OData.Service.Contexts;

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Kompass.OData.ResponseShaping;
using Kompass.OData.Url;

/// <summary>
/// Shared response factory methods. Each context delegates here with the appropriate
/// <c>@odata.context</c> fragment pre-computed from the schema.
/// </summary>
internal static class ODataResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
    };

    internal static IResult Ok(object entity, string contextUrl, SelectClause? select)
    {
        var node = JsonSerializer.SerializeToNode(entity, JsonOptions);
        if (node is not JsonObject obj)
        {
            return Results.Json(entity, statusCode: 200, contentType: "application/json");
        }

        if (select is not null)
        {
            obj = SelectProjector.Project(obj, select);
        }

        var response = new JsonObject();
        response["@odata.context"] = contextUrl;
        foreach (var prop in obj)
        {
            response[prop.Key] = prop.Value?.DeepClone();
        }

        return Results.Content(response.ToJsonString(JsonOptions), "application/json", statusCode: 200);
    }

    internal static IResult OkCollection(
        IEnumerable<object> items, string contextUrl, SelectClause? select,
        long? count, string? nextLink)
    {
        var builder = new ODataResponseBuilder(contextUrl, select);

        foreach (var item in items)
        {
            var node = JsonSerializer.SerializeToNode(item, JsonOptions);
            builder.AddItem(node);
        }

        if (count is not null)
        {
            builder.WithCount(count.Value);
        }

        if (nextLink is not null)
        {
            builder.WithNextLink(nextLink);
        }

        return Results.Content(builder.ToCollectionJson(JsonOptions), "application/json", statusCode: 200);
    }

    internal static IResult Created(object entity, string contextUrl, SelectClause? select)
    {
        var node = JsonSerializer.SerializeToNode(entity, JsonOptions);
        if (node is not JsonObject obj)
        {
            return Results.Json(entity, statusCode: 201, contentType: "application/json");
        }

        if (select is not null)
        {
            obj = SelectProjector.Project(obj, select);
        }

        var response = new JsonObject();
        response["@odata.context"] = contextUrl;
        foreach (var prop in obj)
        {
            response[prop.Key] = prop.Value?.DeepClone();
        }

        return Results.Content(response.ToJsonString(JsonOptions), "application/json", statusCode: 201);
    }

    internal static IResult NotFound() => Results.NotFound();

    internal static IResult NoContent() => Results.NoContent();

    internal static IResult BadRequest(string message) =>
        Results.BadRequest(new { error = new { message } });
}

/// <summary>
/// Base class for all OData handler contexts. Carries the entity set name,
/// raw query string (parsed lazily on first access), request body stream,
/// and the pre-computed <c>@odata.context</c> URL fragment.
/// </summary>
public abstract class ODataContext
{
    private readonly string _rawQuery;
    private QueryOptions? _parsedQuery;

    public string EntitySet { get; }

    /// <summary>The raw request body stream. Read it however you like (JSON, text, binary).</summary>
    public Stream Body { get; }

    internal string ContextUrl { get; }

    /// <summary>Parsed query options. The raw query string is parsed on first access.</summary>
    public QueryOptions Query => _parsedQuery ??= QueryOptions.Parse(_rawQuery);

    internal ODataContext(string entitySet, string rawQuery, Stream body, string contextUrl)
    {
        EntitySet = entitySet;
        _rawQuery = rawQuery;
        Body = body;
        ContextUrl = contextUrl;
    }

    /// <summary>Convenience: read the entire body as a string.</summary>
    public async Task<string> ReadBodyAsStringAsync()
    {
        using var reader = new StreamReader(Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    /// <summary>Convenience: deserialize the body as JSON.</summary>
    public async Task<T?> ReadBodyAsJsonAsync<T>()
    {
        return await JsonSerializer.DeserializeAsync<T>(Body);
    }

    /// <summary>Return a 404 Not Found.</summary>
    public IResult NotFound() => ODataResponse.NotFound();

    /// <summary>Return a 400 Bad Request.</summary>
    public IResult BadRequest(string message) => ODataResponse.BadRequest(message);
}

/// <summary>
/// Context for collection-level operations (GET list, POST create).
/// </summary>
public sealed class CollectionContext : ODataContext
{
    internal CollectionContext(string entitySet, string rawQuery, Stream body, string contextUrl)
        : base(entitySet, rawQuery, body, contextUrl)
    {
    }

    /// <summary>Return a 200 collection response with proper OData envelope.</summary>
    public IResult Ok(IEnumerable<object> items, long? count = null, string? nextLink = null)
    {
        return ODataResponse.OkCollection(items, ContextUrl, Query.Select, count, nextLink);
    }

    /// <summary>Return a 201 Created response for the newly created entity.</summary>
    public IResult Created(object entity)
    {
        return ODataResponse.Created(entity, $"{ContextUrl}/$entity", Query.Select);
    }
}

/// <summary>
/// Context for single-entity operations (GET by key, PATCH, DELETE).
/// </summary>
public sealed class EntityContext : ODataContext
{
    public string Key { get; }

    internal EntityContext(string entitySet, string key, string rawQuery, Stream body, string contextUrl)
        : base(entitySet, rawQuery, body, contextUrl)
    {
        Key = key;
    }

    /// <summary>Return a 200 OK response with the entity.</summary>
    public IResult Ok(object entity)
    {
        return ODataResponse.Ok(entity, ContextUrl, Query.Select);
    }

    /// <summary>Return a 204 No Content (e.g. after successful DELETE).</summary>
    public IResult NoContent() => ODataResponse.NoContent();
}

/// <summary>
/// Context for contained navigation collection operations.
/// </summary>
public sealed class ContainedCollectionContext : ODataContext
{
    public string ParentKey { get; }
    public string NavProp { get; }

    internal ContainedCollectionContext(
        string entitySet, string parentKey, string navProp,
        string rawQuery, Stream body, string contextUrl)
        : base(entitySet, rawQuery, body, contextUrl)
    {
        ParentKey = parentKey;
        NavProp = navProp;
    }

    /// <summary>Return a 200 collection response with proper OData envelope.</summary>
    public IResult Ok(IEnumerable<object> items, long? count = null, string? nextLink = null)
    {
        return ODataResponse.OkCollection(items, ContextUrl, Query.Select, count, nextLink);
    }

    /// <summary>Return a 201 Created response for the newly created entity.</summary>
    public IResult Created(object entity)
    {
        return ODataResponse.Created(entity, $"{ContextUrl}/$entity", Query.Select);
    }
}

/// <summary>
/// Context for contained navigation single-entity operations.
/// </summary>
public sealed class ContainedEntityContext : ODataContext
{
    public string ParentKey { get; }
    public string NavProp { get; }
    public string Key { get; }

    internal ContainedEntityContext(
        string entitySet, string parentKey, string navProp, string key,
        string rawQuery, Stream body, string contextUrl)
        : base(entitySet, rawQuery, body, contextUrl)
    {
        ParentKey = parentKey;
        NavProp = navProp;
        Key = key;
    }

    /// <summary>Return a 200 OK response with the entity.</summary>
    public IResult Ok(object entity)
    {
        return ODataResponse.Ok(entity, ContextUrl, Query.Select);
    }

    /// <summary>Return a 204 No Content.</summary>
    public IResult NoContent() => ODataResponse.NoContent();
}

namespace Kompass.OData.ResponseShaping;

using System.Text.Json;
using System.Text.Json.Nodes;
using Kompass.OData.Url;

/// <summary>
/// Builds OData-compliant JSON response envelopes.
/// </summary>
public sealed class ODataResponseBuilder
{
    private readonly string? _contextUrl;
    private readonly List<JsonNode?> _items = new List<JsonNode?>();
    private long? _count;
    private string? _nextLink;
    private readonly SelectClause? _select;

    public ODataResponseBuilder(string? contextUrl = null, SelectClause? select = null)
    {
        _contextUrl = contextUrl;
        _select = select;
    }

    /// <summary>
    /// Set the total count for the response (when $count=true).
    /// </summary>
    public ODataResponseBuilder WithCount(long count)
    {
        _count = count;
        return this;
    }

    /// <summary>
    /// Set the next link for server-driven paging.
    /// </summary>
    public ODataResponseBuilder WithNextLink(string nextLink)
    {
        _nextLink = nextLink;
        return this;
    }

    /// <summary>
    /// Add a single entity (as a JsonNode) to the response collection.
    /// </summary>
    public ODataResponseBuilder AddItem(JsonNode? item)
    {
        if (_select is not null && item is JsonObject obj)
        {
            _items.Add(SelectProjector.Project(obj, _select));
        }
        else
        {
            _items.Add(item);
        }
        return this;
    }

    /// <summary>
    /// Add multiple entities to the response collection.
    /// </summary>
    public ODataResponseBuilder AddItems(IEnumerable<JsonNode?> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
        return this;
    }

    /// <summary>
    /// Build the collection response envelope.
    /// </summary>
    public JsonObject BuildCollectionResponse()
    {
        var response = new JsonObject();

        if (_contextUrl is not null)
        {
            response["@odata.context"] = _contextUrl;
        }

        if (_count is not null)
        {
            response["@odata.count"] = _count.Value;
        }

        var array = new JsonArray();
        foreach (var item in _items)
        {
            array.Add(item?.DeepClone());
        }
        response["value"] = array;

        if (_nextLink is not null)
        {
            response["@odata.nextLink"] = _nextLink;
        }

        return response;
    }

    /// <summary>
    /// Build a single-entity response (no collection envelope).
    /// </summary>
    public JsonObject BuildEntityResponse(JsonObject entity)
    {
        var response = entity.DeepClone().AsObject();

        if (_contextUrl is not null)
        {
            // Insert @odata.context at the beginning
            var withContext = new JsonObject();
            withContext["@odata.context"] = _contextUrl;
            foreach (var prop in response)
            {
                withContext[prop.Key] = prop.Value?.DeepClone();
            }
            return withContext;
        }

        return response;
    }

    /// <summary>
    /// Serialize the collection response to a JSON string.
    /// </summary>
    public string ToCollectionJson(JsonSerializerOptions? options = null)
    {
        return BuildCollectionResponse().ToJsonString(options ?? DefaultOptions);
    }

    /// <summary>
    /// Serialize a single-entity response to a JSON string.
    /// </summary>
    public string ToEntityJson(JsonObject entity, JsonSerializerOptions? options = null)
    {
        return BuildEntityResponse(entity).ToJsonString(options ?? DefaultOptions);
    }

    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
    };
}

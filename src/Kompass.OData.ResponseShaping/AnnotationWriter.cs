namespace Kompass.OData.ResponseShaping;

using System.Text.Json.Nodes;

/// <summary>
/// Writes OData annotations (@odata.id, @odata.editLink, @odata.type)
/// into entity JSON objects.
/// </summary>
public static class AnnotationWriter
{
    /// <summary>
    /// Add standard OData instance annotations to an entity.
    /// </summary>
    public static JsonObject AddInstanceAnnotations(
        JsonObject entity,
        string odataId,
        string? editLink = null,
        string? odataType = null)
    {
        entity["@odata.id"] = odataId;

        if (editLink is not null)
        {
            entity["@odata.editLink"] = editLink;
        }

        if (odataType is not null)
        {
            entity["@odata.type"] = odataType;
        }

        return entity;
    }

    /// <summary>
    /// Generate the @odata.id for an entity in a given entity set with the specified key.
    /// </summary>
    public static string BuildODataId(string baseUrl, string entitySetName, string key)
    {
        return $"{baseUrl.TrimEnd('/')}/{entitySetName}('{key}')";
    }

    /// <summary>
    /// Generate the @odata.id for a contained entity.
    /// </summary>
    public static string BuildContainedODataId(
        string baseUrl, string entitySetName, string parentKey,
        string navPropName, string key)
    {
        return $"{baseUrl.TrimEnd('/')}/{entitySetName}('{parentKey}')/{navPropName}('{key}')";
    }
}

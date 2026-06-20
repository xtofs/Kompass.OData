namespace Kompass.OData.ResponseShaping;

using System.Text.Json.Nodes;
using Kompass.OData.Url;

/// <summary>
/// Projects an entity's properties based on $select, keeping only
/// the selected properties and always preserving key properties and OData annotations.
/// </summary>
public static class SelectProjector
{
    /// <summary>
    /// Project a JSON object according to a $select clause.
    /// Properties whose names appear in <paramref name="select"/> are kept;
    /// all others are removed. Properties starting with '@' (OData annotations) are always kept.
    /// </summary>
    public static JsonObject Project(JsonObject entity, SelectClause select)
    {
        var selectedNames = new HashSet<string>(select.Items, StringComparer.OrdinalIgnoreCase);
        var result = new JsonObject();

        foreach (var prop in entity)
        {
            // Always keep OData annotations
            if (prop.Key.StartsWith('@'))
            {
                result[prop.Key] = prop.Value?.DeepClone();
                continue;
            }

            if (selectedNames.Contains(prop.Key))
            {
                result[prop.Key] = prop.Value?.DeepClone();
            }
        }

        return result;
    }

    /// <summary>
    /// Project a collection of JSON objects according to a $select clause.
    /// </summary>
    public static IEnumerable<JsonObject> ProjectAll(
        IEnumerable<JsonObject> entities, SelectClause select)
    {
        foreach (var entity in entities)
        {
            yield return Project(entity, select);
        }
    }
}

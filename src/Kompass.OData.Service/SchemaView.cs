namespace Kompass.OData.Service;

using Kompass.CsdlEdm.Edm;

/// <summary>
/// Internal projection of the resolved EDM model into a router-oriented working set.
/// </summary>
internal sealed class SchemaView
{
    internal string Namespace { get; }
    internal IReadOnlyList<EntitySetView> EntitySets { get; }
    internal IReadOnlyList<EntityTypeView> EntityTypes { get; }

    private SchemaView(string ns, List<EntitySetView> entitySets, List<EntityTypeView> entityTypes)
    {
        Namespace = ns;
        EntitySets = entitySets;
        EntityTypes = entityTypes;
    }

    internal EntitySetView? FindEntitySet(string name)
    {
        foreach (var es in EntitySets)
        {
            if (es.Name == name)
            {
                return es;
            }
        }
        return null;
    }

    internal EntityTypeView? FindEntityType(string name)
    {
        foreach (var et in EntityTypes)
        {
            if (et.Name == name)
            {
                return et;
            }
        }
        return null;
    }

    internal static SchemaView FromModel(Model model)
    {
        var entityTypes = new List<EntityTypeView>();
        var entitySets = new List<EntitySetView>();

        foreach (var element in model.Elements)
        {
            if (element is EntityType et)
            {
                var containedNavs = new List<NavigationPropertyView>();
                foreach (var nav in et.NavigationProperties)
                {
                    if (nav.ContainsTarget == true)
                    {
                        containedNavs.Add(new NavigationPropertyView(nav.Name, nav.Target.Name));
                    }
                }
                entityTypes.Add(new EntityTypeView(et.Name, containedNavs));
            }
        }

        if (model.EntityContainer is not null)
        {
            foreach (var elem in model.EntityContainer.Elements)
            {
                if (elem is EntitySet esElem)
                {
                    entitySets.Add(new EntitySetView(esElem.Name, esElem.Target.Name));
                }
            }
        }

        return new SchemaView(model.Namespace, entitySets, entityTypes);
    }
}

internal sealed class EntitySetView
{
    internal string Name { get; }
    internal string EntityTypeName { get; }

    internal EntitySetView(string name, string entityTypeName)
    {
        Name = name;
        EntityTypeName = entityTypeName;
    }
}

internal sealed class EntityTypeView
{
    internal string Name { get; }
    internal IReadOnlyList<NavigationPropertyView> ContainedNavigationProperties { get; }

    internal EntityTypeView(string name, IReadOnlyList<NavigationPropertyView> containedNavigationProperties)
    {
        Name = name;
        ContainedNavigationProperties = containedNavigationProperties;
    }
}

internal sealed class NavigationPropertyView
{
    internal string Name { get; }
    internal string TargetTypeName { get; }

    internal NavigationPropertyView(string name, string targetTypeName)
    {
        Name = name;
        TargetTypeName = targetTypeName;
    }
}

namespace Kompass.OData.Service;

using Kompass.OData.Service.Contexts;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Configuration for an entity set's handler registrations.
/// </summary>
public sealed class EntitySetConfig<TState>
{
    internal Func<CollectionContext, TState, Task<IResult>>? ListHandler { get; private set; }
    internal Func<EntityContext, TState, Task<IResult>>? GetHandler { get; private set; }
    internal Func<CollectionContext, TState, Task<IResult>>? CreateHandler { get; private set; }
    internal Func<EntityContext, TState, Task<IResult>>? UpdateHandler { get; private set; }
    internal Func<EntityContext, TState, Task<IResult>>? DeleteHandler { get; private set; }
    internal Dictionary<string, ContainedNavConfig<TState>> ContainedNavs { get; } = new Dictionary<string, ContainedNavConfig<TState>>();

    public EntitySetConfig<TState> OnList(Func<CollectionContext, TState, Task<IResult>> handler)
    {
        ListHandler = handler;
        return this;
    }

    public EntitySetConfig<TState> OnGet(Func<EntityContext, TState, Task<IResult>> handler)
    {
        GetHandler = handler;
        return this;
    }

    public EntitySetConfig<TState> OnCreate(Func<CollectionContext, TState, Task<IResult>> handler)
    {
        CreateHandler = handler;
        return this;
    }

    public EntitySetConfig<TState> OnUpdate(Func<EntityContext, TState, Task<IResult>> handler)
    {
        UpdateHandler = handler;
        return this;
    }

    public EntitySetConfig<TState> OnDelete(Func<EntityContext, TState, Task<IResult>> handler)
    {
        DeleteHandler = handler;
        return this;
    }

    public EntitySetConfig<TState> ContainedCollection(string navPropName, Func<ContainedNavConfig<TState>, ContainedNavConfig<TState>> configure)
    {
        var config = new ContainedNavConfig<TState>();
        ContainedNavs[navPropName] = configure(config);
        return this;
    }
}

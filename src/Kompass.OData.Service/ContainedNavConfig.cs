namespace Kompass.OData.Service;

using Kompass.OData.Service.Contexts;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Configuration for a contained navigation property's handler registrations.
/// </summary>
public sealed class ContainedNavConfig<TState>
{
    internal Func<ContainedCollectionContext, TState, Task<IResult>>? ListHandler { get; private set; }
    internal Func<ContainedEntityContext, TState, Task<IResult>>? GetHandler { get; private set; }
    internal Func<ContainedCollectionContext, TState, Task<IResult>>? CreateHandler { get; private set; }
    internal Func<ContainedEntityContext, TState, Task<IResult>>? UpdateHandler { get; private set; }
    internal Func<ContainedEntityContext, TState, Task<IResult>>? DeleteHandler { get; private set; }

    public ContainedNavConfig<TState> OnList(Func<ContainedCollectionContext, TState, Task<IResult>> handler)
    {
        ListHandler = handler;
        return this;
    }

    public ContainedNavConfig<TState> OnGet(Func<ContainedEntityContext, TState, Task<IResult>> handler)
    {
        GetHandler = handler;
        return this;
    }

    public ContainedNavConfig<TState> OnCreate(Func<ContainedCollectionContext, TState, Task<IResult>> handler)
    {
        CreateHandler = handler;
        return this;
    }

    public ContainedNavConfig<TState> OnUpdate(Func<ContainedEntityContext, TState, Task<IResult>> handler)
    {
        UpdateHandler = handler;
        return this;
    }

    public ContainedNavConfig<TState> OnDelete(Func<ContainedEntityContext, TState, Task<IResult>> handler)
    {
        DeleteHandler = handler;
        return this;
    }
}

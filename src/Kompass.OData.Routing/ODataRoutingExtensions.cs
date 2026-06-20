namespace Kompass.OData.Routing;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// ASP.NET Core middleware that rewrites OData subsegment-key URLs into segment form
/// and stashes the original URI in <see cref="HttpContext.Items"/>.
/// </summary>
public sealed class ODataPathRewriteMiddleware
{
    private readonly RequestDelegate _next;

    public ODataPathRewriteMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var originalPath = context.Request.Path.Value ?? "/";
        var rewrittenPath = ODataPathRewriter.RewritePath(originalPath);

        if (rewrittenPath != originalPath)
        {
            // Stash the original URI before rewriting
            var originalUri = new Uri(
                $"{context.Request.Scheme}://{context.Request.Host}{originalPath}{context.Request.QueryString}");
            context.Items[typeof(OriginalODataUri)] = new OriginalODataUri(originalUri);

            context.Request.Path = rewrittenPath;

            // Clear the cached route values so routing re-evaluates with new path
            context.SetEndpoint(null);
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering OData path rewriting.
/// </summary>
public static class ODataRoutingExtensions
{
    /// <summary>
    /// Adds the OData path rewrite middleware to the pipeline.
    /// In .NET 8+ WebApplication, this must be called before UseRouting() to ensure
    /// the rewrite happens before route matching. For WebApplication (which implicitly
    /// adds routing), use <see cref="UseODataPathRewriteWithRouting"/> instead.
    /// </summary>
    public static IApplicationBuilder UseODataPathRewrite(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ODataPathRewriteMiddleware>();
    }

    /// <summary>
    /// Adds OData path rewriting AND explicit routing in the correct order.
    /// Use this with WebApplication (Minimal API host) to ensure the rewrite
    /// middleware runs before the routing middleware.
    /// </summary>
    public static WebApplication UseODataPathRewriteWithRouting(this WebApplication app)
    {
        // Use inline middleware that runs at exactly this point in the pipeline,
        // before the implicit UseRouting.
        app.Use(async (context, next) =>
        {
            var originalPath = context.Request.Path.Value ?? "/";
            var rewrittenPath = ODataPathRewriter.RewritePath(originalPath);

            if (rewrittenPath != originalPath)
            {
                var originalUri = new Uri(
                    $"{context.Request.Scheme}://{context.Request.Host}{originalPath}{context.Request.QueryString}");
                context.Items[typeof(OriginalODataUri)] = new OriginalODataUri(originalUri);

                context.Request.Path = rewrittenPath;
            }

            await next(context);
        });

        return app;
    }

    /// <summary>
    /// Retrieves the original OData URI from before path rewriting, if available.
    /// </summary>
    public static OriginalODataUri? GetOriginalODataUri(this HttpContext context)
    {
        return context.Items[typeof(OriginalODataUri)] as OriginalODataUri;
    }


    /// <summary>
    /// Prints all registered OData routes to the console.
    /// Routes with <c>__key__</c> sentinels are converted to parenthesized OData key notation.
    /// Must be called after all routes are registered (e.g. in ApplicationStarted).
    /// </summary>
    public static void PrintRegisteredRoutes(this WebApplication app)
    {
         var routes = new List<ODataRouteInfo>();

        foreach (var dataSource in app.Services.GetRequiredService<IEnumerable<EndpointDataSource>>())
        {
            foreach (var endpoint in dataSource.Endpoints)
            {
                if (endpoint is RouteEndpoint routeEndpoint)
                {
                    var raw = routeEndpoint.RoutePattern.RawText ?? "";
                    var odataPath = ODataPathRewriter.FormatAsODataPath(raw);
                    var methods = routeEndpoint.Metadata
                        .OfType<HttpMethodMetadata>()
                        .FirstOrDefault()?.HttpMethods;
                    var method = methods is not null ? string.Join(",", methods) : "???";
                    routes.Add(new ODataRouteInfo(method, odataPath));
                }
            }
        }

        var note =  string.Join(Environment.NewLine, routes.Select(r => $"{r.Methods} {r.Path}"));

        app.Logger.LogInformation("Registered routes:{nl}{note}", Environment.NewLine,note);

        // Console.WriteLine("Registered routes:");
        // foreach (var route in routes)
        // {
        //     logger.LogInformation("  {methods,-7} {path}", route.Methods, route.Path);
        // }
    }
}

/// <summary>
/// Describes a registered OData route with its HTTP method and path.
/// </summary>
public sealed record ODataRouteInfo(string Methods, string Path);

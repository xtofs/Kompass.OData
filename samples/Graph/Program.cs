namespace Kompass.OData.Samples.Graph;

using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Kompass.OData.Routing;
using Kompass.OData.Service;
using Kompass.OData.Service.Contexts;

public static class Program
{
    // In-memory data
    private static readonly List<JsonObject> Users =
    [
        new JsonObject
        {
            ["Id"] = "u1",
            ["DisplayName"] = "Adele Vance",
            ["Mail"] = "adele@example.com",
            ["UserPrincipalName"] = "adele@example.com",
            ["JobTitle"] = "Engineer",
            ["Messages"] = new JsonArray(
                new JsonObject { ["Id"] = "m1", ["Subject"] = "Hello", ["IsRead"] = true },
                new JsonObject { ["Id"] = "m2", ["Subject"] = "Meeting", ["IsRead"] = false }),
            ["Events"] = new JsonArray(
                new JsonObject { ["Id"] = "e1", ["Subject"] = "Standup", ["IsAllDay"] = false }),
        },
        new JsonObject
        {
            ["Id"] = "u2",
            ["DisplayName"] = "Alex Wilber",
            ["Mail"] = "alex@example.com",
            ["UserPrincipalName"] = "alex@example.com",
            ["JobTitle"] = "Designer",
            ["Messages"] = new JsonArray(),
            ["Events"] = new JsonArray(),
        },
    ];

    private static readonly List<JsonObject> Groups =
    [
        new JsonObject { ["Id"] = "g1", ["DisplayName"] = "Engineering", ["MailEnabled"] = true, ["SecurityEnabled"] = true },
        new JsonObject { ["Id"] = "g2", ["DisplayName"] = "Marketing", ["MailEnabled"] = true, ["SecurityEnabled"] = false },
    ];

    private static readonly List<JsonObject> Drives =
    [
        new JsonObject
        {
            ["Id"] = "d1",
            ["Name"] = "OneDrive",
            ["DriveType"] = "personal",
            ["Items"] = new JsonArray(
                new JsonObject { ["Id"] = "di1", ["Name"] = "Document.docx", ["Size"] = 1024 },
                new JsonObject { ["Id"] = "di2", ["Name"] = "Spreadsheet.xlsx", ["Size"] = 2048 }),
        },
    ];

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        var csdlXml = File.ReadAllText("graph.csdl.xml");
        var service = ODataServiceBuilder.FromCsdl(csdlXml)
            .EntitySet("Users", es => es
                .OnList(ListUsers)
                .OnGet(GetUser)
                .ContainedCollection("Messages", nav => nav
                    .OnList(ListMessages)
                    .OnGet(GetMessage))
                .ContainedCollection("Events", nav => nav
                    .OnList(ListEvents)
                    .OnGet(GetEvent)))
            .EntitySet("Groups", es => es
                .OnList(ListGroups)
                .OnGet(GetGroup))
            .EntitySet("Drives", es => es
                .OnList(ListDrives)
                .OnGet(GetDrive)
                .ContainedCollection("Items", nav => nav
                    .OnList(ListDriveItems)
                    .OnGet(GetDriveItem)));

        app.UseODataPathRewriteWithRouting();
        app.UseRouting();
        service.MapODataEndpoints(app);


        app.MapGet("/", () => service.GenerateServiceDocument());
        app.Lifetime.ApplicationStarted.Register(() => app.PrintRegisteredRoutes());
        app.Run();
    }

    // --- Users ---

    private static Task<IResult> ListUsers(CollectionContext ctx, IServiceProvider sp)
    {
        var items = Users.AsEnumerable();
        if (ctx.Query.Page.Skip is not null) { items = items.Skip((int)ctx.Query.Page.Skip.Value); }
        if (ctx.Query.Page.Top is not null) { items = items.Take((int)ctx.Query.Page.Top.Value); }

        var count = ctx.Query.Count == true ? (long?)Users.Count : null;
        return Task.FromResult(ctx.Ok(items.Cast<object>(), count));
    }

    private static Task<IResult> GetUser(EntityContext ctx, IServiceProvider sp)
    {
        var user = Users.FirstOrDefault(u => u["Id"]?.GetValue<string>() == ctx.Key);
        if (user is null) { return Task.FromResult(ctx.NotFound()); }
        return Task.FromResult(ctx.Ok(user));
    }

    // --- Messages (contained in Users) ---

    private static Task<IResult> ListMessages(ContainedCollectionContext ctx, IServiceProvider sp)
    {
        var parent = Users.FirstOrDefault(u => u["Id"]?.GetValue<string>() == ctx.ParentKey);
        if (parent is null) { return Task.FromResult(ctx.NotFound()); }

        var messages = parent["Messages"]?.AsArray() ?? [];
        return Task.FromResult(ctx.Ok(messages.Select(m => (object)(m?.DeepClone()!))));
    }

    private static Task<IResult> GetMessage(ContainedEntityContext ctx, IServiceProvider sp)
    {
        var parent = Users.FirstOrDefault(u => u["Id"]?.GetValue<string>() == ctx.ParentKey);
        if (parent is null) { return Task.FromResult(ctx.NotFound()); }

        var messages = parent["Messages"]?.AsArray();
        var msg = messages?.FirstOrDefault(m => m?["Id"]?.GetValue<string>() == ctx.Key);
        if (msg is null) { return Task.FromResult(ctx.NotFound()); }
        return Task.FromResult(ctx.Ok(msg));
    }

    // --- Events (contained in Users) ---

    private static Task<IResult> ListEvents(ContainedCollectionContext ctx, IServiceProvider sp)
    {
        var parent = Users.FirstOrDefault(u => u["Id"]?.GetValue<string>() == ctx.ParentKey);
        if (parent is null) { return Task.FromResult(ctx.NotFound()); }

        var events = parent["Events"]?.AsArray() ?? [];
        return Task.FromResult(ctx.Ok(events.Select(e => (object)(e?.DeepClone()!))));
    }

    private static Task<IResult> GetEvent(ContainedEntityContext ctx, IServiceProvider sp)
    {
        var parent = Users.FirstOrDefault(u => u["Id"]?.GetValue<string>() == ctx.ParentKey);
        if (parent is null) { return Task.FromResult(ctx.NotFound()); }

        var events = parent["Events"]?.AsArray();
        var evt = events?.FirstOrDefault(e => e?["Id"]?.GetValue<string>() == ctx.Key);
        if (evt is null) { return Task.FromResult(ctx.NotFound()); }
        return Task.FromResult(ctx.Ok(evt));
    }

    // --- Groups ---

    private static Task<IResult> ListGroups(CollectionContext ctx, IServiceProvider sp)
    {
        var items = Groups.AsEnumerable();
        if (ctx.Query.Page.Skip is not null) { items = items.Skip((int)ctx.Query.Page.Skip.Value); }
        if (ctx.Query.Page.Top is not null) { items = items.Take((int)ctx.Query.Page.Top.Value); }

        var count = ctx.Query.Count == true ? (long?)Groups.Count : null;
        return Task.FromResult(ctx.Ok(items.Cast<object>(), count));
    }

    private static Task<IResult> GetGroup(EntityContext ctx, IServiceProvider sp)
    {
        var group = Groups.FirstOrDefault(g => g["Id"]?.GetValue<string>() == ctx.Key);
        if (group is null) { return Task.FromResult(ctx.NotFound()); }
        return Task.FromResult(ctx.Ok(group));
    }

    // --- Drives ---

    private static Task<IResult> ListDrives(CollectionContext ctx, IServiceProvider sp)
    {
        var items = Drives.AsEnumerable();
        if (ctx.Query.Page.Skip is not null) { items = items.Skip((int)ctx.Query.Page.Skip.Value); }
        if (ctx.Query.Page.Top is not null) { items = items.Take((int)ctx.Query.Page.Top.Value); }

        var count = ctx.Query.Count == true ? (long?)Drives.Count : null;
        return Task.FromResult(ctx.Ok(items.Cast<object>(), count));
    }

    private static Task<IResult> GetDrive(EntityContext ctx, IServiceProvider sp)
    {
        var drive = Drives.FirstOrDefault(d => d["Id"]?.GetValue<string>() == ctx.Key);
        if (drive is null) { return Task.FromResult(ctx.NotFound()); }
        return Task.FromResult(ctx.Ok(drive));
    }

    // --- Drive Items (contained in Drives) ---

    private static Task<IResult> ListDriveItems(ContainedCollectionContext ctx, IServiceProvider sp)
    {
        var parent = Drives.FirstOrDefault(d => d["Id"]?.GetValue<string>() == ctx.ParentKey);
        if (parent is null) { return Task.FromResult(ctx.NotFound()); }

        var items = parent["Items"]?.AsArray() ?? [];
        return Task.FromResult(ctx.Ok(items.Select(i => (object)(i?.DeepClone()!))));
    }

    private static Task<IResult> GetDriveItem(ContainedEntityContext ctx, IServiceProvider sp)
    {
        var parent = Drives.FirstOrDefault(d => d["Id"]?.GetValue<string>() == ctx.ParentKey);
        if (parent is null) { return Task.FromResult(ctx.NotFound()); }

        var items = parent["Items"]?.AsArray();
        var item = items?.FirstOrDefault(i => i?["Id"]?.GetValue<string>() == ctx.Key);
        if (item is null) { return Task.FromResult(ctx.NotFound()); }
        return Task.FromResult(ctx.Ok(item));
    }
}

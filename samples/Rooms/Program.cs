namespace Kompass.OData.Samples.Rooms;

using Microsoft.AspNetCore.Http;
using Kompass.OData.Routing;
using Kompass.OData.Service;
using Kompass.OData.Service.Contexts;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<RoomsRepository>();

        var app = builder.Build();



        var csdlXml = File.ReadAllText("rooms.csdl.xml");

        var service = ODataServiceBuilder.FromCsdl<RoomsRepository>(csdlXml)
            .EntitySet("Rooms", es => es
                .OnList(ListRooms)
                .OnGet(GetRoom)
                .ContainedCollection("Printers", nav => nav
                    .OnList(ListPrinters)
                    .OnGet(GetPrinter))
                .ContainedCollection("Phones", nav => nav
                    .OnList(ListPhones)
                    .OnGet(GetPhone)));

        foreach (var warning in service.GetWarnings())
        {
            Console.WriteLine($"Warning: {warning}");
        }

        app.UseODataPathRewriteWithRouting();
        app.UseRouting();
        service.Build(app);

        app.MapGet("/", () => Results.Content(
            service.GenerateServiceDocument("https://localhost:5000"),
            "application/json"));

        app.Lifetime.ApplicationStarted.Register(() => app.PrintODataRoutes());
        app.Run();
    }

    private static async Task<IResult> ListRooms(CollectionContext ctx, RoomsRepository repo)
    {
        var (items, totalCount) = await repo.GetRoomsAsync(
            skip: (int?)ctx.Query.Page.Skip,
            top: (int?)ctx.Query.Page.Top);

        var count = ctx.Query.Count == true ? (long?)totalCount : null;
        return ctx.Ok(items.Cast<object>(), count);
    }

    private static async Task<IResult> GetRoom(EntityContext ctx, RoomsRepository repo)
    {
        var room = await repo.GetRoomAsync(ctx.Key);
        return room is not null ? ctx.Ok(room) : ctx.NotFound();
    }

    private static async Task<IResult> ListPrinters(ContainedCollectionContext ctx, RoomsRepository repo)
    {
        var printers = await repo.GetPrintersAsync(ctx.ParentKey);
        return printers is not null ? ctx.Ok(printers.Cast<object>()) : ctx.NotFound();
    }

    private static async Task<IResult> GetPrinter(ContainedEntityContext ctx, RoomsRepository repo)
    {
        var printer = await repo.GetPrinterAsync(ctx.ParentKey, ctx.Key);
        return printer is not null ? ctx.Ok(printer) : ctx.NotFound();
    }

    private static async Task<IResult> ListPhones(ContainedCollectionContext ctx, RoomsRepository repo)
    {
        var phones = await repo.GetPhonesAsync(ctx.ParentKey);
        return phones is not null ? ctx.Ok(phones.Cast<object>()) : ctx.NotFound();
    }

    private static async Task<IResult> GetPhone(ContainedEntityContext ctx, RoomsRepository repo)
    {
        var phone = await repo.GetPhoneAsync(ctx.ParentKey, ctx.Key);
        return phone is not null ? ctx.Ok(phone) : ctx.NotFound();
    }
}

namespace Kompass.OData.Samples.Rooms;

using System.Text.Json.Nodes;

/// <summary>
/// In-memory async repository for rooms and their contained entities.
/// </summary>
public sealed class RoomsRepository
{
    // public RoomsRepository(){}

    private readonly List<JsonObject> _rooms =
    [
        CreateRoom("oak-204", "Oak 204", [("hp-1", "HP LaserJet"), ("hp-2", "HP DeskJet")], [("p-1", "x100", "555-0101")]),
        CreateRoom("maple-301", "Maple 301", [("ep-1", "Epson WF")], []),
        CreateRoom("birch-102", "Birch 102", [], [("p-2", "x200", "555-0202"), ("p-3", "x201", "555-0203")]),
    ];

    // --- Rooms ---

    public async Task<(IReadOnlyList<JsonObject> Items, long TotalCount)> GetRoomsAsync(int? skip = null, int? top = null)
    {
        await Task.CompletedTask;
        var items = _rooms.AsEnumerable();
        if (skip is not null)
        {
            items = items.Skip(skip.Value);
        }
        if (top is not null)
        {
            items = items.Take(top.Value);
        }
        return (items.ToList(), _rooms.Count);
    }

    public async Task<JsonObject?> GetRoomAsync(string id)
    {
        await Task.CompletedTask;
        return _rooms.FirstOrDefault(r => r["Id"]?.GetValue<string>() == id);
    }

    // --- Printers (contained in Room) ---

    public async Task<IReadOnlyList<JsonObject>?> GetPrintersAsync(string roomId)
    {
        var room = await GetRoomAsync(roomId);
        if (room is null)
        {
            return null;
        }
        var printers = room["Printers"]?.AsArray() ?? new JsonArray();
        return printers.Select(p => (JsonObject)p!.DeepClone()).ToList();
    }

    public async Task<JsonObject?> GetPrinterAsync(string roomId, string printerId)
    {
        var room = await GetRoomAsync(roomId);
        if (room is null)
        {
            return null;
        }
        var printers = room["Printers"]?.AsArray();
        var printer = printers?.FirstOrDefault(p => p?["Id"]?.GetValue<string>() == printerId);
        return printer is not null ? (JsonObject)printer.DeepClone() : null;
    }

    // --- Phones (contained in Room) ---

    public async Task<IReadOnlyList<JsonObject>?> GetPhonesAsync(string roomId)
    {
        var room = await GetRoomAsync(roomId);
        if (room is null)
        {
            return null;
        }
        var phones = room["Phones"]?.AsArray() ?? new JsonArray();
        return phones.Select(p => (JsonObject)p!.DeepClone()).ToList();
    }

    public async Task<JsonObject?> GetPhoneAsync(string roomId, string phoneId)
    {
        var room = await GetRoomAsync(roomId);
        if (room is null)
        {
            return null;
        }
        var phones = room["Phones"]?.AsArray();
        var phone = phones?.FirstOrDefault(p => p?["Id"]?.GetValue<string>() == phoneId);
        return phone is not null ? (JsonObject)phone.DeepClone() : null;
    }

    // --- Seed data ---

    private static JsonObject CreateRoom(
        string id, string name,
        (string Id, string Model)[] printers,
        (string Id, string Extension, string Number)[] phones)
    {
        var printersArray = new JsonArray();
        foreach (var (pId, pModel) in printers)
        {
            printersArray.Add(new JsonObject
            {
                ["Id"] = pId,
                ["Model"] = pModel,
            });
        }

        var phonesArray = new JsonArray();
        foreach (var (phId, phExt, phNum) in phones)
        {
            phonesArray.Add(new JsonObject
            {
                ["Id"] = phId,
                ["Extension"] = phExt,
                ["Number"] = phNum,
            });
        }

        return new JsonObject
        {
            ["Id"] = id,
            ["Name"] = name,
            ["Printers"] = printersArray,
            ["Phones"] = phonesArray,
        };
    }
}

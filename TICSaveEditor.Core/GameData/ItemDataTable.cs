namespace TICSaveEditor.Core.GameData;

internal sealed class ItemDataTable
{
    private readonly Dictionary<int, ItemInfo> _byId;

    public ItemDataTable(IReadOnlyList<ItemInfo> entries)
    {
        Entries = entries;
        _byId = entries.ToDictionary(e => e.Id);
    }

    public IReadOnlyList<ItemInfo> Entries { get; }

    public bool TryGet(int id, out ItemInfo? info)
    {
        if (_byId.TryGetValue(id, out var entry))
        {
            info = entry;
            return true;
        }
        info = null;
        return false;
    }

    public string GetName(int id)
    {
        if (_byId.TryGetValue(id, out var entry) && !string.IsNullOrEmpty(entry.Name))
            return entry.Name;
        return $"Unknown Item (ID {id})";
    }

    public static ItemDataTable Empty { get; } = new(Array.Empty<ItemInfo>());
}

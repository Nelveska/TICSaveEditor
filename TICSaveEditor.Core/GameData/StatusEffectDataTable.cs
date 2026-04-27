namespace TICSaveEditor.Core.GameData;

internal sealed class StatusEffectDataTable
{
    private readonly Dictionary<int, StatusEffectInfo> _byId;

    public StatusEffectDataTable(IReadOnlyList<StatusEffectInfo> entries)
    {
        Entries = entries;
        _byId = entries.ToDictionary(e => e.Id);
    }

    public IReadOnlyList<StatusEffectInfo> Entries { get; }

    public bool TryGet(int id, out StatusEffectInfo? info)
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
        return $"Unknown StatusEffect (ID {id})";
    }

    public static StatusEffectDataTable Empty { get; } = new(Array.Empty<StatusEffectInfo>());
}

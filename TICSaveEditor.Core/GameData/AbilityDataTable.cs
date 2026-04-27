namespace TICSaveEditor.Core.GameData;

internal sealed class AbilityDataTable
{
    private readonly Dictionary<int, AbilityInfo> _byId;

    public AbilityDataTable(IReadOnlyList<AbilityInfo> entries)
    {
        Entries = entries;
        _byId = entries.ToDictionary(e => e.Id);
    }

    public IReadOnlyList<AbilityInfo> Entries { get; }

    public bool TryGet(int id, out AbilityInfo? info)
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
        return $"Unknown Ability (ID {id})";
    }

    public static AbilityDataTable Empty { get; } = new(Array.Empty<AbilityInfo>());
}

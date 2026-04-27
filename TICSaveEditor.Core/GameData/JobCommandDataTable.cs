namespace TICSaveEditor.Core.GameData;

internal sealed class JobCommandDataTable
{
    private readonly Dictionary<int, JobCommandInfo> _byId;

    public JobCommandDataTable(IReadOnlyList<JobCommandInfo> entries)
    {
        Entries = entries;
        _byId = entries.ToDictionary(e => e.Id);
    }

    public IReadOnlyList<JobCommandInfo> Entries { get; }

    public bool TryGet(int id, out JobCommandInfo? info)
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
        return $"Unknown Command (ID {id})";
    }

    public static JobCommandDataTable Empty { get; } = new(Array.Empty<JobCommandInfo>());
}

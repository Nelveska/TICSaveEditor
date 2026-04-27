namespace TICSaveEditor.Core.GameData;

internal sealed class JobDataTable
{
    private readonly Dictionary<int, JobInfo> _byId;

    public JobDataTable(IReadOnlyList<JobInfo> entries)
    {
        Entries = entries;
        _byId = entries.ToDictionary(e => e.Id);
    }

    public IReadOnlyList<JobInfo> Entries { get; }

    public bool TryGet(int id, out JobInfo? info)
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
        return $"Unknown Job (ID {id})";
    }

    public static JobDataTable Empty { get; } = new(Array.Empty<JobInfo>());
}

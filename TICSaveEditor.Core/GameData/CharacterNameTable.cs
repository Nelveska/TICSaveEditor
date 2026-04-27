namespace TICSaveEditor.Core.GameData;

internal sealed class CharacterNameTable
{
    private readonly Dictionary<ushort, CharacterNameInfo> _byNameNo;

    public CharacterNameTable(IReadOnlyList<CharacterNameInfo> entries)
    {
        Entries = entries;
        _byNameNo = entries.ToDictionary(e => e.NameNo);
    }

    public IReadOnlyList<CharacterNameInfo> Entries { get; }

    public bool TryGet(ushort nameNo, out CharacterNameInfo? info)
    {
        if (_byNameNo.TryGetValue(nameNo, out var entry))
        {
            info = entry;
            return true;
        }
        info = null;
        return false;
    }

    public string GetName(ushort nameNo)
    {
        if (_byNameNo.TryGetValue(nameNo, out var entry) && !string.IsNullOrEmpty(entry.Name))
            return entry.Name;
        return $"Unknown Character (NameNo {nameNo})";
    }

    public static CharacterNameTable Empty { get; } = new(Array.Empty<CharacterNameInfo>());
}

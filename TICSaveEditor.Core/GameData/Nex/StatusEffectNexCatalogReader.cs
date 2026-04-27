namespace TICSaveEditor.Core.GameData.Nex;

/// <summary>
/// Reads <c>UIStatusEffect.json</c> (DB Browser export of the <c>UIStatusEffect-en</c> table).
/// Maps to <c>StatusEffectInfo</c> in the C# domain. Per decisions_m7x_remaining_tables.md:
/// the modloader's <c>StatusEffectData.xml</c> is NOT used — its Id space is the game-mechanics
/// table (40 entries, Ids 0..39) which doesn't align with the UI table indexed here. v0.1 uses
/// Nex Ids exclusively.
/// </summary>
internal sealed class StatusEffectNexCatalogReader
{
    private const string TableLabel = "UIStatusEffect";

    public IReadOnlyList<StatusEffectNexEntry> Read(Stream jsonStream)
    {
        var (rows, columnIndices) = NexCatalogParser.Parse(jsonStream, TableLabel);

        int idIdx = NexCatalogParser.RequireColumn(columnIndices, "Key", TableLabel);
        int nameIdx = NexCatalogParser.RequireColumn(columnIndices, "Name", TableLabel);
        // UIStatusEffect uses "Caption" for the long-form description (analogous to other tables' "Description").
        int captionIdx = NexCatalogParser.RequireColumn(columnIndices, "Caption", TableLabel);
        int typeIdx = NexCatalogParser.RequireColumn(columnIndices, "Type", TableLabel);

        var result = new List<StatusEffectNexEntry>(rows.Count);
        for (int rowNum = 0; rowNum < rows.Count; rowNum++)
        {
            var row = rows[rowNum]?.AsArray()
                ?? throw new InvalidDataException($"{TableLabel}.json rows[{rowNum}] is not a JSON array.");

            result.Add(new StatusEffectNexEntry(
                Id: NexCatalogParser.ReadInt(row, idIdx, "Key", rowNum, TableLabel),
                Name: NexCatalogParser.ReadStringOrEmpty(row, nameIdx),
                Description: NexCatalogParser.ReadStringOrEmpty(row, captionIdx),
                Type: NexCatalogParser.ReadByteOrZero(row, typeIdx, "Type", rowNum, TableLabel)));
        }
        return result;
    }
}

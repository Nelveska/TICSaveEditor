namespace TICSaveEditor.Core.GameData.Nex;

internal sealed class CharaNameNexCatalogReader
{
    private const string TableLabel = "CharaName";

    public IReadOnlyList<CharaNameNexEntry> Read(Stream jsonStream)
    {
        var (rows, columnIndices) = NexCatalogParser.Parse(jsonStream, TableLabel);

        int keyIdx = NexCatalogParser.RequireColumn(columnIndices, "Key", TableLabel);
        int nameIdx = NexCatalogParser.RequireColumn(columnIndices, "Name", TableLabel);
        int isGenericIdx = NexCatalogParser.RequireColumn(columnIndices, "IsGeneric", TableLabel);

        var result = new List<CharaNameNexEntry>(rows.Count);
        for (int rowNum = 0; rowNum < rows.Count; rowNum++)
        {
            var row = rows[rowNum]?.AsArray()
                ?? throw new InvalidDataException($"{TableLabel}.json rows[{rowNum}] is not a JSON array.");

            result.Add(new CharaNameNexEntry(
                NameNo: NexCatalogParser.ReadUShort(row, keyIdx, "Key", rowNum, TableLabel),
                Name: NexCatalogParser.ReadStringOrEmpty(row, nameIdx),
                IsGeneric: NexCatalogParser.ReadBool(row, isGenericIdx)));
        }
        return result;
    }
}

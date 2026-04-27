namespace TICSaveEditor.Core.GameData.Nex;

internal sealed class ItemNexCatalogReader
{
    private const string TableLabel = "Item";

    public IReadOnlyList<ItemNexEntry> Read(Stream jsonStream)
    {
        var (rows, columnIndices) = NexCatalogParser.Parse(jsonStream, TableLabel);

        int idIdx = NexCatalogParser.RequireColumn(columnIndices, "Key", TableLabel);
        int nameIdx = NexCatalogParser.RequireColumn(columnIndices, "Name", TableLabel);
        int descriptionIdx = NexCatalogParser.RequireColumn(columnIndices, "Description", TableLabel);
        int nameSingularIdx = NexCatalogParser.RequireColumn(columnIndices, "NameSingular", TableLabel);
        int namePluralIdx = NexCatalogParser.RequireColumn(columnIndices, "NamePlural", TableLabel);

        var result = new List<ItemNexEntry>(rows.Count);
        for (int rowNum = 0; rowNum < rows.Count; rowNum++)
        {
            var row = rows[rowNum]?.AsArray()
                ?? throw new InvalidDataException($"{TableLabel}.json rows[{rowNum}] is not a JSON array.");

            result.Add(new ItemNexEntry(
                Id: NexCatalogParser.ReadInt(row, idIdx, "Key", rowNum, TableLabel),
                Name: NexCatalogParser.ReadStringOrEmpty(row, nameIdx),
                Description: NexCatalogParser.ReadStringOrEmpty(row, descriptionIdx),
                NameSingular: NexCatalogParser.ReadStringOrEmpty(row, nameSingularIdx),
                NamePlural: NexCatalogParser.ReadStringOrEmpty(row, namePluralIdx)));
        }
        return result;
    }
}

namespace TICSaveEditor.Core.GameData.Nex;

internal sealed class AbilityNexCatalogReader
{
    private const string TableLabel = "Ability";

    public IReadOnlyList<AbilityNexEntry> Read(Stream jsonStream)
    {
        var (rows, columnIndices) = NexCatalogParser.Parse(jsonStream, TableLabel);

        int idIdx = NexCatalogParser.RequireColumn(columnIndices, "Key", TableLabel);
        int nameIdx = NexCatalogParser.RequireColumn(columnIndices, "Name", TableLabel);
        int descriptionIdx = NexCatalogParser.RequireColumn(columnIndices, "Description", TableLabel);
        // JpCost1 = enhanced mode (per decisions_m7x_remaining_tables.md). JpCost2 (classic) deferred to v0.2.
        int jpCostIdx = NexCatalogParser.RequireColumn(columnIndices, "JpCost1", TableLabel);

        var result = new List<AbilityNexEntry>(rows.Count);
        for (int rowNum = 0; rowNum < rows.Count; rowNum++)
        {
            var row = rows[rowNum]?.AsArray()
                ?? throw new InvalidDataException($"{TableLabel}.json rows[{rowNum}] is not a JSON array.");

            result.Add(new AbilityNexEntry(
                Id: NexCatalogParser.ReadInt(row, idIdx, "Key", rowNum, TableLabel),
                Name: NexCatalogParser.ReadStringOrEmpty(row, nameIdx),
                Description: NexCatalogParser.ReadStringOrEmpty(row, descriptionIdx),
                JpCost: NexCatalogParser.ReadIntOrZero(row, jpCostIdx)));
        }
        return result;
    }
}

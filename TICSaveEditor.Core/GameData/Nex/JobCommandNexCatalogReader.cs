namespace TICSaveEditor.Core.GameData.Nex;

internal sealed class JobCommandNexCatalogReader
{
    private const string TableLabel = "JobCommand";

    public IReadOnlyList<JobCommandNexEntry> Read(Stream jsonStream)
    {
        var (rows, columnIndices) = NexCatalogParser.Parse(jsonStream, TableLabel);

        int idIdx = NexCatalogParser.RequireColumn(columnIndices, "Key", TableLabel);
        int nameIdx = NexCatalogParser.RequireColumn(columnIndices, "Name", TableLabel);
        int descriptionIdx = NexCatalogParser.RequireColumn(columnIndices, "Description", TableLabel);

        var result = new List<JobCommandNexEntry>(rows.Count);
        for (int rowNum = 0; rowNum < rows.Count; rowNum++)
        {
            var row = rows[rowNum]?.AsArray()
                ?? throw new InvalidDataException($"{TableLabel}.json rows[{rowNum}] is not a JSON array.");

            result.Add(new JobCommandNexEntry(
                Id: NexCatalogParser.ReadInt(row, idIdx, "Key", rowNum, TableLabel),
                Name: NexCatalogParser.ReadStringOrEmpty(row, nameIdx),
                Description: NexCatalogParser.ReadStringOrEmpty(row, descriptionIdx)));
        }
        return result;
    }
}

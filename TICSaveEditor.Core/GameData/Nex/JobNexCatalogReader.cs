namespace TICSaveEditor.Core.GameData.Nex;

internal sealed class JobNexCatalogReader
{
    private const string TableLabel = "Job";
    private const string ColKey = "Key";
    private const string ColName = "Name";
    private const string ColDescription = "Description";
    private const string ColJobTypeId = "jobtype+Id";
    private const string ColJobCommandId = "jobcommand+Id";

    public IReadOnlyList<JobNexEntry> Read(Stream jsonStream)
    {
        var (rows, columnIndices) = NexCatalogParser.Parse(jsonStream, TableLabel);

        int idIdx = NexCatalogParser.RequireColumn(columnIndices, ColKey, TableLabel);
        int nameIdx = NexCatalogParser.RequireColumn(columnIndices, ColName, TableLabel);
        int descriptionIdx = NexCatalogParser.RequireColumn(columnIndices, ColDescription, TableLabel);
        int jobTypeIdx = NexCatalogParser.RequireColumn(columnIndices, ColJobTypeId, TableLabel);
        int jobCommandIdx = NexCatalogParser.RequireColumn(columnIndices, ColJobCommandId, TableLabel);

        var result = new List<JobNexEntry>(rows.Count);
        for (int rowNum = 0; rowNum < rows.Count; rowNum++)
        {
            var row = rows[rowNum]?.AsArray()
                ?? throw new InvalidDataException(
                    $"{TableLabel}.json rows[{rowNum}] is not a JSON array.");

            result.Add(new JobNexEntry(
                Id: NexCatalogParser.ReadInt(row, idIdx, ColKey, rowNum, TableLabel),
                Name: NexCatalogParser.ReadStringOrEmpty(row, nameIdx),
                Description: NexCatalogParser.ReadStringOrEmpty(row, descriptionIdx),
                JobTypeId: NexCatalogParser.ReadInt(row, jobTypeIdx, ColJobTypeId, rowNum, TableLabel),
                JobCommandId: NexCatalogParser.ReadInt(row, jobCommandIdx, ColJobCommandId, rowNum, TableLabel)));
        }
        return result;
    }
}

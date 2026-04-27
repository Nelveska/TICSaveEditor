using System.Text;
using TICSaveEditor.Core.GameData.Nex;

namespace TICSaveEditor.Core.Tests.GameData;

public class JobNexCatalogReaderTests
{
    private static Stream Json(string body) =>
        new MemoryStream(Encoding.UTF8.GetBytes(body));

    private const string MinimalSquireBody = """
        {
            "type": "table",
            "name": "Job-en",
            "columns": [
                {"name": "Key", "type": "INTEGER"},
                {"name": "Name", "type": "TEXT"},
                {"name": "Description", "type": "TEXT"},
                {"name": "jobtype+Id", "type": "INTEGER"},
                {"name": "jobcommand+Id", "type": "INTEGER"}
            ],
            "rows": [
                [1, "Squire", "This job serves as the foundation.", 1, 25]
            ]
        }
        """;

    [Fact]
    public void Read_parses_id_name_description_jobtype_jobcommand_from_DB_Browser_shape()
    {
        var reader = new JobNexCatalogReader();
        var entries = reader.Read(Json(MinimalSquireBody));
        var squire = Assert.Single(entries);

        Assert.Equal(1, squire.Id);
        Assert.Equal("Squire", squire.Name);
        Assert.Equal("This job serves as the foundation.", squire.Description);
        Assert.Equal(1, squire.JobTypeId);
        Assert.Equal(25, squire.JobCommandId);
    }

    [Fact]
    public void Read_handles_jobcommand_plus_Id_column_name_verbatim()
    {
        // Confirms the `+` separator in column names doesn't trip up the lookup.
        var reader = new JobNexCatalogReader();
        var entries = reader.Read(Json(MinimalSquireBody));
        Assert.Equal(25, entries[0].JobCommandId);
    }

    [Fact]
    public void Read_treats_null_Name_and_null_Description_as_empty_string()
    {
        var body = """
            {
                "type": "table",
                "columns": [
                    {"name": "Key", "type": "INTEGER"},
                    {"name": "Name", "type": "TEXT"},
                    {"name": "Description", "type": "TEXT"},
                    {"name": "jobtype+Id", "type": "INTEGER"},
                    {"name": "jobcommand+Id", "type": "INTEGER"}
                ],
                "rows": [
                    [0, null, null, 0, 0]
                ]
            }
            """;
        var reader = new JobNexCatalogReader();
        var entry = Assert.Single(reader.Read(Json(body)));
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.Description);
    }

    [Fact]
    public void Read_throws_when_required_column_missing()
    {
        // Drop the "Name" column; reader must throw with a useful message.
        var body = """
            {
                "type": "table",
                "columns": [
                    {"name": "Key", "type": "INTEGER"},
                    {"name": "Description", "type": "TEXT"},
                    {"name": "jobtype+Id", "type": "INTEGER"},
                    {"name": "jobcommand+Id", "type": "INTEGER"}
                ],
                "rows": []
            }
            """;
        var reader = new JobNexCatalogReader();
        var ex = Assert.Throws<InvalidDataException>(() => reader.Read(Json(body)));
        Assert.Contains("Name", ex.Message);
    }

    [Fact]
    public void Read_returns_empty_list_when_rows_is_empty()
    {
        var body = """
            {
                "type": "table",
                "columns": [
                    {"name": "Key", "type": "INTEGER"},
                    {"name": "Name", "type": "TEXT"},
                    {"name": "Description", "type": "TEXT"},
                    {"name": "jobtype+Id", "type": "INTEGER"},
                    {"name": "jobcommand+Id", "type": "INTEGER"}
                ],
                "rows": []
            }
            """;
        var reader = new JobNexCatalogReader();
        Assert.Empty(reader.Read(Json(body)));
    }

    [Fact]
    public void Read_throws_when_columns_array_missing()
    {
        var body = """{"type":"table","rows":[]}""";
        var reader = new JobNexCatalogReader();
        Assert.Throws<InvalidDataException>(() => reader.Read(Json(body)));
    }

    [Fact]
    public void Read_against_bundled_Job_json_finds_Squire_at_id_1()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "TICSaveEditor.Core", "Resources", "Nex", "en", "Job.json");
        if (!File.Exists(path)) return; // file not yet committed

        using var stream = File.OpenRead(path);
        var entries = new JobNexCatalogReader().Read(stream);

        var squire = entries.FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(squire);
        Assert.Equal("Squire", squire!.Name);
        Assert.Equal(25, squire.JobCommandId);
        Assert.Equal(1, squire.JobTypeId);
    }
}

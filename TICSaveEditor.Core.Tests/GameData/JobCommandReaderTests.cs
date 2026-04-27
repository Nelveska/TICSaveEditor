using TICSaveEditor.Core.GameData.Nex;

namespace TICSaveEditor.Core.Tests.GameData;

public class JobCommandReaderTests
{
    private static string ResourcePath(string relative) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "TICSaveEditor.Core", "Resources", relative);

    [Fact]
    public void JobCommandNexCatalogReader_parses_bundled_returns_entries()
    {
        var path = ResourcePath(Path.Combine("Nex", "en", "JobCommand.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new JobCommandNexCatalogReader().Read(stream);
        Assert.NotEmpty(entries);
        Assert.Contains(entries, e => !string.IsNullOrEmpty(e.Name));
    }

    [Fact]
    public void JobCommandNexCatalogReader_id_25_resolves_to_named_command()
    {
        // Squire's JobCommandId is 25 (from Job.json + JobCommandData.xml cross-reference).
        // The Nex JobCommand entry at Id=25 should have a non-empty Name (likely "Mettle").
        var path = ResourcePath(Path.Combine("Nex", "en", "JobCommand.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new JobCommandNexCatalogReader().Read(stream);
        var cmd = entries.FirstOrDefault(e => e.Id == 25);
        Assert.NotNull(cmd);
        Assert.False(string.IsNullOrEmpty(cmd!.Name),
            $"JobCommand Id=25 expected non-empty Name; got '{cmd.Name}'.");
    }
}

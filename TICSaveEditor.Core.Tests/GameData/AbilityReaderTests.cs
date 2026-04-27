using TICSaveEditor.Core.GameData.Nex;
using TICSaveEditor.Core.GameData.Xml;

namespace TICSaveEditor.Core.Tests.GameData;

public class AbilityReaderTests
{
    private static string ResourcePath(string relative) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "TICSaveEditor.Core", "Resources", relative);

    [Fact]
    public void AbilityDataXmlReader_returns_at_least_100_entries_from_bundled()
    {
        var path = ResourcePath(Path.Combine("Modloader", "AbilityData.xml"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new AbilityDataXmlReader().Read(stream);
        Assert.True(entries.Count >= 100, $"Expected ≥100 abilities; got {entries.Count}.");
    }

    [Fact]
    public void AbilityDataXmlReader_cure_at_id_1_has_normal_type_chance_90()
    {
        var path = ResourcePath(Path.Combine("Modloader", "AbilityData.xml"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new AbilityDataXmlReader().Read(stream);
        var cure = entries.FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(cure);
        Assert.Equal("Normal", cure!.AbilityType);
        Assert.Equal((byte)90, cure.ChanceToLearn);
    }

    [Fact]
    public void AbilityNexCatalogReader_parses_bundled_with_JpCost1()
    {
        var path = ResourcePath(Path.Combine("Nex", "en", "Ability.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new AbilityNexCatalogReader().Read(stream);
        Assert.True(entries.Count >= 100);
        Assert.Contains(entries, e => e.JpCost > 0);
    }
}

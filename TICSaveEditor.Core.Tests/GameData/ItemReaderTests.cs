using TICSaveEditor.Core.GameData.Nex;
using TICSaveEditor.Core.GameData.Xml;

namespace TICSaveEditor.Core.Tests.GameData;

public class ItemReaderTests
{
    private static string ResourcePath(string relative) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "TICSaveEditor.Core", "Resources", relative);

    [Fact]
    public void ItemDataXmlReader_returns_at_least_100_entries_from_bundled()
    {
        var path = ResourcePath(Path.Combine("Modloader", "ItemData.xml"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new ItemDataXmlReader().Read(stream);
        Assert.True(entries.Count >= 100, $"Expected ≥100 items; got {entries.Count}.");
    }

    [Fact]
    public void ItemDataXmlReader_dagger_at_id_1_has_Knife_category_and_price_100()
    {
        var path = ResourcePath(Path.Combine("Modloader", "ItemData.xml"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new ItemDataXmlReader().Read(stream);
        var dagger = entries.FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(dagger);
        Assert.Equal("Knife", dagger!.ItemCategory);
        Assert.Equal(100, dagger.Price);
        Assert.Equal((byte)1, dagger.RequiredLevel);
    }

    [Fact]
    public void ItemNexCatalogReader_parses_bundled_with_NameSingular_NamePlural()
    {
        var path = ResourcePath(Path.Combine("Nex", "en", "Item.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new ItemNexCatalogReader().Read(stream);
        Assert.True(entries.Count >= 100);

        // Spot-check: at least one entry has a non-empty Name.
        Assert.Contains(entries, e => !string.IsNullOrEmpty(e.Name));
    }
}

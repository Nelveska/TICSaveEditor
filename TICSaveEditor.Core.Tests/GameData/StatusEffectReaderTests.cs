using TICSaveEditor.Core.GameData.Nex;

namespace TICSaveEditor.Core.Tests.GameData;

public class StatusEffectReaderTests
{
    private static string ResourcePath(string relative) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "TICSaveEditor.Core", "Resources", relative);

    [Fact]
    public void StatusEffectNexCatalogReader_parses_UIStatusEffect_returns_entries()
    {
        var path = ResourcePath(Path.Combine("Nex", "en", "UIStatusEffect.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new StatusEffectNexCatalogReader().Read(stream);
        Assert.NotEmpty(entries);
    }

    [Fact]
    public void StatusEffectNexCatalogReader_id_3_is_KO()
    {
        // Per UIStatusEffect-en inspection: Id=3 has Name="KO".
        var path = ResourcePath(Path.Combine("Nex", "en", "UIStatusEffect.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new StatusEffectNexCatalogReader().Read(stream);
        var ko = entries.FirstOrDefault(e => e.Id == 3);
        Assert.NotNull(ko);
        Assert.Equal("KO", ko!.Name);
        Assert.False(string.IsNullOrEmpty(ko.Description));
    }
}

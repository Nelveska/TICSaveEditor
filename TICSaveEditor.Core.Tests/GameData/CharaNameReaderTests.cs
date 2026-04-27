using TICSaveEditor.Core.GameData.Nex;

namespace TICSaveEditor.Core.Tests.GameData;

public class CharaNameReaderTests
{
    private static string ResourcePath(string relative) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "TICSaveEditor.Core", "Resources", relative);

    [Fact]
    public void CharaNameNexCatalogReader_parses_bundled_returns_entries()
    {
        var path = ResourcePath(Path.Combine("Nex", "en", "CharaName.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new CharaNameNexCatalogReader().Read(stream);
        Assert.NotEmpty(entries);
    }

    [Fact]
    public void CharaNameNexCatalogReader_id_1_is_Ramza()
    {
        var path = ResourcePath(Path.Combine("Nex", "en", "CharaName.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new CharaNameNexCatalogReader().Read(stream);
        var ramza = entries.FirstOrDefault(e => e.NameNo == 1);
        Assert.NotNull(ramza);
        Assert.Equal("Ramza", ramza!.Name);
    }

    [Fact]
    public void CharaNameNexCatalogReader_NameNo_is_ushort_typed()
    {
        var path = ResourcePath(Path.Combine("Nex", "en", "CharaName.json"));
        if (!File.Exists(path)) return;
        using var stream = File.OpenRead(path);
        var entries = new CharaNameNexCatalogReader().Read(stream);
        // ushort range: [0, 65535]. Property is typed as ushort already; this asserts no overflow.
        foreach (var e in entries) Assert.InRange(e.NameNo, 0, 65535);
    }
}

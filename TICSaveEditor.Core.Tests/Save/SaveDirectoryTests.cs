using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Tests.Fixtures;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveDirectoryTests : IDisposable
{
    private readonly string _tempDir;

    public SaveDirectoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tic-savedir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
        }
    }

    [Fact]
    public void Scan_classifies_files_by_kind()
    {
        File.WriteAllBytes(Path.Combine(_tempDir, "manual1.png"), SyntheticSaveBuilder.BuildManualSavePng());
        File.WriteAllBytes(Path.Combine(_tempDir, "manual2.png"), SyntheticSaveBuilder.BuildManualSavePng());
        File.WriteAllBytes(Path.Combine(_tempDir, "enwm_main.sav"), SyntheticSaveBuilder.BuildResumeWorldSaveRaw());
        File.WriteAllBytes(Path.Combine(_tempDir, "enwm_attack.sav"), SyntheticSaveBuilder.BuildResumeBattleSaveRaw());

        var dir = SaveDirectory.Scan(_tempDir);

        Assert.Equal(4, dir.Files.Count);
        Assert.Equal(2, dir.Files.Count(f => f.Kind == SaveFileKind.Manual));
        Assert.Equal(1, dir.Files.Count(f => f.Kind == SaveFileKind.ResumeWorld));
        Assert.Equal(1, dir.Files.Count(f => f.Kind == SaveFileKind.ResumeBattle));
    }

    [Fact]
    public void Scan_marks_resume_battle_as_not_editable()
    {
        File.WriteAllBytes(Path.Combine(_tempDir, "enwm_attack.sav"), SyntheticSaveBuilder.BuildResumeBattleSaveRaw());
        var dir = SaveDirectory.Scan(_tempDir);
        var battle = dir.Files.Single(f => f.Kind == SaveFileKind.ResumeBattle);
        Assert.False(battle.IsEditable);
    }

    [Fact]
    public void Scan_marks_manual_as_editable()
    {
        File.WriteAllBytes(Path.Combine(_tempDir, "manual.png"), SyntheticSaveBuilder.BuildManualSavePng());
        var dir = SaveDirectory.Scan(_tempDir);
        var manual = dir.Files.Single();
        Assert.True(manual.IsEditable);
    }

    [Fact]
    public void Scan_skips_steam_autocloud_vdf()
    {
        File.WriteAllText(Path.Combine(_tempDir, "steam_autocloud.vdf"), "steam metadata");
        File.WriteAllBytes(Path.Combine(_tempDir, "manual.png"), SyntheticSaveBuilder.BuildManualSavePng());

        var dir = SaveDirectory.Scan(_tempDir);

        Assert.Single(dir.Files);
        Assert.Equal("manual.png", dir.Files[0].FileName);
    }

    [Fact]
    public void Scan_skips_files_in_editor_backups()
    {
        var backupDir = Path.Combine(_tempDir, ".editor-backups", "20260425T120000Z");
        Directory.CreateDirectory(backupDir);
        File.WriteAllBytes(Path.Combine(backupDir, "old.png"), SyntheticSaveBuilder.BuildManualSavePng());
        File.WriteAllBytes(Path.Combine(_tempDir, "current.png"), SyntheticSaveBuilder.BuildManualSavePng());

        var dir = SaveDirectory.Scan(_tempDir);

        Assert.Single(dir.Files);
        Assert.Equal("current.png", dir.Files[0].FileName);
    }

    [Fact]
    public void Scan_detects_name_clash_filenames()
    {
        var clashName = "save (1 Name clash 2026-04-25).png";
        File.WriteAllBytes(Path.Combine(_tempDir, clashName), SyntheticSaveBuilder.BuildManualSavePng());
        File.WriteAllBytes(Path.Combine(_tempDir, "regular.png"), SyntheticSaveBuilder.BuildManualSavePng());

        var dir = SaveDirectory.Scan(_tempDir);

        Assert.True(dir.Files.Single(f => f.FileName == clashName).IsNameClashRename);
        Assert.False(dir.Files.Single(f => f.FileName == "regular.png").IsNameClashRename);
    }

    [Fact]
    public void Scan_throws_on_missing_directory()
    {
        var bogus = Path.Combine(_tempDir, "does-not-exist");
        Assert.Throws<DirectoryNotFoundException>(() => SaveDirectory.Scan(bogus));
    }

    // Locks the M10 smoke-test fix: PeekKind must UMIF-unpack PNG payloads
    // before reading the discriminator. Prior to the fix, every PNG fell
    // through to SaveFileKind.Manual because offset 0x08 of the UMIF header
    // is the magic 0x46494D55 ("UMIF"), never 0x10.
    [Fact]
    public void Scan_classifies_real_baseline_fixture_files()
    {
        var fixtureDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "SaveFiles", "Baseline"));
        if (!Directory.Exists(fixtureDir)) return;

        var dir = SaveDirectory.Scan(fixtureDir);
        var enhanced = dir.Files.SingleOrDefault(f => f.FileName == "enhanced.png");
        Assert.NotNull(enhanced);
        Assert.Equal(SaveFileKind.Manual, enhanced!.Kind);
        Assert.True(enhanced.IsEditable);

        // autoenhanced.png is the in-progress battle auto-save; it's optional in test
        // fixtures (the 2026-05-01 fixture set is enhanced.png only). When present, we
        // still verify the ResumeBattle classification path.
        var auto = dir.Files.SingleOrDefault(f => f.FileName == "autoenhanced.png");
        if (auto != null)
        {
            Assert.Equal(SaveFileKind.ResumeBattle, auto.Kind);
            Assert.False(auto.IsEditable);
        }
    }

}

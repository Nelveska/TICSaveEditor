using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Tests.Fixtures;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveFileLoaderBackupTests : IDisposable
{
    private readonly string _tempDir;

    public SaveFileLoaderBackupTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tic-loader-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { }
    }

    [Fact]
    public void Load_string_triggers_backup()
    {
        var path = Path.Combine(_tempDir, "manual.png");
        File.WriteAllBytes(path, SyntheticSaveBuilder.BuildManualSavePng());

        SaveFileLoader.Load(path);

        var backupRoot = Path.Combine(_tempDir, ".editor-backups");
        Assert.True(Directory.Exists(backupRoot));
        Assert.NotEmpty(Directory.GetDirectories(backupRoot));
    }

    [Fact]
    public void Load_byte_array_does_not_trigger_backup()
    {
        var path = Path.Combine(_tempDir, "manual.png");
        var bytes = SyntheticSaveBuilder.BuildManualSavePng();
        File.WriteAllBytes(path, bytes);

        SaveFileLoader.Load(bytes, path);

        var backupRoot = Path.Combine(_tempDir, ".editor-backups");
        Assert.False(Directory.Exists(backupRoot));
    }
}

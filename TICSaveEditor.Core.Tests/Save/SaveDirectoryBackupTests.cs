using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Tests.Fixtures;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveDirectoryBackupTests : IDisposable
{
    private readonly string _tempDir;

    public SaveDirectoryBackupTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tic-backup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { }
    }

    [Fact]
    public void BackupSiblings_copies_png_and_resume_files_and_vdf()
    {
        var manualPath = Path.Combine(_tempDir, "manual.png");
        File.WriteAllBytes(manualPath, SyntheticSaveBuilder.BuildManualSavePng());
        File.WriteAllBytes(Path.Combine(_tempDir, "enwm_main.sav"), SyntheticSaveBuilder.BuildResumeWorldSaveRaw());
        File.WriteAllText(Path.Combine(_tempDir, "steam_autocloud.vdf"), "metadata");
        File.WriteAllText(Path.Combine(_tempDir, "ignored.txt"), "not a save");

        var backup = new SaveDirectoryBackup(new BackupOptions());
        var result = backup.BackupSiblings(manualPath);

        Assert.False(result.Skipped);
        Assert.Equal(3, result.FilesBackedUp.Count);
        Assert.True(Directory.Exists(result.BackupDirectory));
        Assert.True(File.Exists(Path.Combine(result.BackupDirectory, "manual.png")));
        Assert.True(File.Exists(Path.Combine(result.BackupDirectory, "enwm_main.sav")));
        Assert.True(File.Exists(Path.Combine(result.BackupDirectory, "steam_autocloud.vdf")));
        Assert.False(File.Exists(Path.Combine(result.BackupDirectory, "ignored.txt")));
    }

    [Fact]
    public void BackupSiblings_dedupes_when_called_twice_in_a_row()
    {
        var manualPath = Path.Combine(_tempDir, "manual.png");
        File.WriteAllBytes(manualPath, SyntheticSaveBuilder.BuildManualSavePng());

        var backup = new SaveDirectoryBackup(new BackupOptions());
        var first = backup.BackupSiblings(manualPath);
        var second = backup.BackupSiblings(manualPath);

        Assert.False(first.Skipped);
        Assert.True(second.Skipped);
        Assert.Empty(second.FilesBackedUp);
    }

    [Fact]
    public void EnforceRetention_deletes_oldest_beyond_max()
    {
        var backupRoot = Path.Combine(_tempDir, ".editor-backups");
        Directory.CreateDirectory(Path.Combine(backupRoot, "20260101T000000Z"));
        Directory.CreateDirectory(Path.Combine(backupRoot, "20260102T000000Z"));
        Directory.CreateDirectory(Path.Combine(backupRoot, "20260103T000000Z"));

        var backup = new SaveDirectoryBackup(new BackupOptions(MaxRetention: 2));
        backup.EnforceRetention(_tempDir);

        var remaining = Directory.GetDirectories(backupRoot)
            .Select(Path.GetFileName)
            .OrderBy(n => n)
            .ToArray();
        Assert.Equal(new[] { "20260102T000000Z", "20260103T000000Z" }, remaining);
    }

    [Fact]
    public void EnforceRetention_no_op_when_under_max()
    {
        var backupRoot = Path.Combine(_tempDir, ".editor-backups");
        Directory.CreateDirectory(Path.Combine(backupRoot, "20260101T000000Z"));

        var backup = new SaveDirectoryBackup(new BackupOptions(MaxRetention: 10));
        backup.EnforceRetention(_tempDir);

        Assert.Single(Directory.GetDirectories(backupRoot));
    }
}

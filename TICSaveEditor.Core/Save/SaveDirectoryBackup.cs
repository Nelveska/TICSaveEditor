using System.Globalization;

namespace TICSaveEditor.Core.Save;

public class SaveDirectoryBackup
{
    private const string BackupFolderName = ".editor-backups";
    private const string TimestampFormat = "yyyyMMdd'T'HHmmss'Z'";
    private static readonly TimeSpan DedupWindow = TimeSpan.FromSeconds(5);

    private readonly BackupOptions _options;

    public SaveDirectoryBackup(BackupOptions options)
    {
        _options = options;
    }

    public BackupResult BackupSiblings(string sourceFilePath)
    {
        var saveDir = Path.GetDirectoryName(sourceFilePath)
            ?? throw new ArgumentException(
                "sourceFilePath must include a directory.", nameof(sourceFilePath));

        var backupRoot = Path.Combine(saveDir, BackupFolderName);

        if (HasRecentBackup(backupRoot))
        {
            return new BackupResult(string.Empty, Array.Empty<string>(), Skipped: true);
        }

        var stamp = DateTime.UtcNow.ToString(TimestampFormat, CultureInfo.InvariantCulture);
        var backupDir = Path.Combine(backupRoot, stamp);
        Directory.CreateDirectory(backupDir);

        var copied = new List<string>();
        foreach (var path in Directory.GetFiles(saveDir))
        {
            var name = Path.GetFileName(path);
            if (!ShouldBackup(name))
            {
                continue;
            }
            var dest = Path.Combine(backupDir, name);
            File.Copy(path, dest, overwrite: false);
            copied.Add(dest);
        }

        EnforceRetention(saveDir);
        return new BackupResult(backupDir, copied, Skipped: false);
    }

    public void EnforceRetention(string saveDirectory)
    {
        var backupRoot = Path.Combine(saveDirectory, BackupFolderName);
        if (!Directory.Exists(backupRoot))
        {
            return;
        }

        var folders = Directory.GetDirectories(backupRoot)
            .OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal)
            .ToList();

        var excess = folders.Count - _options.MaxRetention;
        if (excess <= 0)
        {
            return;
        }

        for (var i = 0; i < excess; i++)
        {
            try
            {
                Directory.Delete(folders[i], recursive: true);
            }
            catch
            {
            }
        }
    }

    private static bool HasRecentBackup(string backupRoot)
    {
        if (!Directory.Exists(backupRoot))
        {
            return false;
        }
        var latest = Directory.GetDirectories(backupRoot)
            .Select(p => Path.GetFileName(p))
            .OrderByDescending(n => n, StringComparer.Ordinal)
            .FirstOrDefault();
        if (latest is null)
        {
            return false;
        }
        if (!DateTime.TryParseExact(
                latest, TimestampFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var ts))
        {
            return false;
        }
        return (DateTime.UtcNow - ts) < DedupWindow;
    }

    private static bool ShouldBackup(string name)
        => name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("enwm_", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "steam_autocloud.vdf", StringComparison.OrdinalIgnoreCase);
}

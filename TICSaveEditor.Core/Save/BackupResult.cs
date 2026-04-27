namespace TICSaveEditor.Core.Save;

public record BackupResult(
    string BackupDirectory,
    IReadOnlyList<string> FilesBackedUp,
    bool Skipped);

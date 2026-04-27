namespace TICSaveEditor.Core.Save;

public record SaveFileInfo(
    string Path,
    string FileName,
    SaveFileKind Kind,
    bool IsEditable,
    DateTime LastWriteTime,
    long Size,
    bool IsNameClashRename);

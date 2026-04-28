using System;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// One row in the directory file list. ResumeBattle entries surface with
/// <see cref="IsOpenable"/> = false and are rendered greyed/disabled per
/// <c>decisions_m10_open_flow.md</c>.
/// </summary>
public class SaveFileItemViewModel : ViewModelBase
{
    public SaveFileItemViewModel(SaveFileInfo info)
    {
        Info = info;
    }

    public SaveFileInfo Info { get; }
    public string FileName => Info.FileName;
    public string Path => Info.Path;
    public DateTime LastWriteTime => Info.LastWriteTime;
    public long SizeBytes => Info.Size;
    public bool IsOpenable => Info.IsEditable;

    public string KindLabel => Info.Kind switch
    {
        SaveFileKind.Manual       => "Manual save",
        SaveFileKind.ResumeWorld  => "Resume save (read-only)",
        SaveFileKind.ResumeBattle => "Auto-save (battle, read-only)",
        _ => Info.Kind.ToString(),
    };

    public string Tooltip => IsOpenable
        ? $"{KindLabel} — {SizeBytes:N0} bytes"
        : "Auto-saves cannot be edited in v0.1 (multi-snapshot battle history).";
}

using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// Polymorphic base over the three SaveFile kinds. Subtypes are
/// <see cref="ManualSaveFileViewModel"/>, <see cref="ResumeWorldSaveFileViewModel"/>,
/// <see cref="ResumeBattleSaveFileViewModel"/>. Resolved at runtime via the
/// Application.DataTemplates polymorphic ContentControl pattern (see
/// <c>decisions_m10_view_wiring.md</c>).
/// </summary>
public abstract class SaveFileViewModel : ViewModelBase
{
    protected SaveFileViewModel(SaveFile model)
    {
        Model = model;
    }

    public SaveFile Model { get; }
    public SaveFileKind Kind => Model.Kind;
    public string SourcePath => Model.SourcePath;
    public int Version => Model.Version;
    public uint StoredChecksum => Model.StoredChecksum;
}

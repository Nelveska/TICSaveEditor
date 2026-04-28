using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// Banner-only — never reached through the OpenFile command because
/// <see cref="SaveFileItemViewModel.IsOpenable"/> is false for ResumeBattle. Exists
/// so the polymorphic factory has a return type to hand back if a caller ever
/// constructs one directly (e.g. tests).
/// </summary>
public class ResumeBattleSaveFileViewModel : SaveFileViewModel
{
    public ResumeBattleSaveFileViewModel(ResumeBattleSaveFile model) : base(model) { }

    public string Message => "In-battle save files are read-only and cannot be browsed in v0.1.";
}

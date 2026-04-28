using CommunityToolkit.Mvvm.ComponentModel;

namespace TICSaveEditor.GUI.ViewModels.Dialogs;

public partial class GameRunningDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "FFT is running";

    [ObservableProperty]
    private string _message =
        "Final Fantasy Tactics is currently running. A backup of your save folder " +
        "will be created before opening, but Steam Cloud may overwrite changes if a " +
        "sync occurs after editing.\n\nOpen anyway?";

    /// <summary>Set by the dialog when the user picks "Open anyway".</summary>
    [ObservableProperty] private bool _confirmed;
}

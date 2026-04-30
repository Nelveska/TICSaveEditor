using CommunityToolkit.Mvvm.ComponentModel;

namespace TICSaveEditor.GUI.ViewModels.Dialogs;

public partial class ConfirmDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "Confirm";
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private string _confirmText = "OK";
    [ObservableProperty] private string _cancelText = "Cancel";

    /// <summary>Set by the dialog when the user picks the confirm button.</summary>
    [ObservableProperty] private bool _confirmed;
}

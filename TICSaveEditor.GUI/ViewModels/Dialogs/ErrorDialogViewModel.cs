using CommunityToolkit.Mvvm.ComponentModel;

namespace TICSaveEditor.GUI.ViewModels.Dialogs;

public partial class ErrorDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "Error";
    [ObservableProperty] private string _message = string.Empty;
}

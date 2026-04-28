using Avalonia.Controls;
using Avalonia.Interactivity;
using TICSaveEditor.GUI.ViewModels.Dialogs;

namespace TICSaveEditor.GUI.Views.Dialogs;

public partial class GameRunningDialog : Window
{
    public GameRunningDialog() { InitializeComponent(); }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GameRunningDialogViewModel vm) vm.Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using TICSaveEditor.GUI.ViewModels.Dialogs;

namespace TICSaveEditor.GUI.Views.Dialogs;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog() { InitializeComponent(); }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ConfirmDialogViewModel vm) vm.Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}

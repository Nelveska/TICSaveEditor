using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TICSaveEditor.GUI.Views.Dialogs;

public partial class ErrorDialog : Window
{
    public ErrorDialog() { InitializeComponent(); }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}

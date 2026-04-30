using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TICSaveEditor.GUI.Views.Dialogs;

public partial class OperationResultDialog : Window
{
    public OperationResultDialog() { InitializeComponent(); }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}

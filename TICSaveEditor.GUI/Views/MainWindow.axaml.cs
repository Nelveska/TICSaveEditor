using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using TICSaveEditor.GUI.ViewModels;
using TICSaveEditor.GUI.ViewModels.Dialogs;
using TICSaveEditor.GUI.Views.Dialogs;

namespace TICSaveEditor.GUI.Views;

public partial class MainWindow : Window
{
    private bool _confirmedClose;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Wire view-side Func hooks once the window is fully shown. Opened fires
        // after the visual tree is up AND DataContext has settled, which is more
        // reliable than AttachedToVisualTree (which can race with DataContext
        // assignment in some Avalonia 12.x scenarios — observed empirically when
        // Browse silently failed because PickFolderAsync was never assigned).
        if (DataContext is not MainWindowViewModel vm)
        {
            // Fallback: subscribe to DataContextChanged so wiring still happens
            // if the VM is assigned after Opened fires.
            DataContextChanged += OnDataContextChangedWireHooks;
            return;
        }
        WireHooks(vm);
    }

    private void OnDataContextChangedWireHooks(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            DataContextChanged -= OnDataContextChangedWireHooks;
            WireHooks(vm);
        }
    }

    private void WireHooks(MainWindowViewModel vm)
    {
        vm.PickFolderAsync = async () =>
        {
            var top = GetTopLevel(this);
            if (top is null) return null;
            var folders = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select TIC save folder",
                AllowMultiple = false,
            });
            if (folders.Count == 0) return null;
            return folders[0].TryGetLocalPath();
        };

        vm.ConfirmGameRunningAsync = async () =>
        {
            var dlgVm = new GameRunningDialogViewModel();
            var dlg = new GameRunningDialog { DataContext = dlgVm };
            await dlg.ShowDialog(this);
            return dlgVm.Confirmed;
        };

        vm.ShowErrorAsync = async (message) =>
        {
            var dlg = new ErrorDialog { DataContext = new ErrorDialogViewModel { Message = message } };
            await dlg.ShowDialog(this);
        };

        vm.ConfirmDiscardChangesAsync = async () =>
        {
            var dlgVm = new ConfirmDialogViewModel
            {
                Title = "Unsaved changes",
                Message = "You have unsaved changes. Discard them and continue?",
                ConfirmText = "Discard",
                CancelText = "Cancel",
            };
            var dlg = new ConfirmDialog { DataContext = dlgVm };
            await dlg.ShowDialog(this);
            return dlgVm.Confirmed;
        };

        vm.AskLevelAsync = async () =>
        {
            var dlgVm = new LevelInputDialogViewModel();
            var dlg = new LevelInputDialog { DataContext = dlgVm };
            await dlg.ShowDialog(this);
            return dlgVm.Confirmed ? dlgVm.Level : (int?)null;
        };

        vm.ShowOperationResultAsync = async (label, result) =>
        {
            var dlgVm = new OperationResultDialogViewModel(label, result);
            var dlg = new OperationResultDialog { DataContext = dlgVm };
            await dlg.ShowDialog(this);
        };

        // If a file was loaded before WireHooks ran (default-path scan + auto-open),
        // its slot VMs missed the AskLevelAsync / ShowOperationResultAsync setters.
        // Re-propagate now that the funcs are assigned.
        vm.RewireOpenFileSlotFuncs();
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_confirmedClose) return;
        if (DataContext is not MainWindowViewModel vm) return;
        if (!vm.IsDirty) return;

        e.Cancel = true;
        var ok = await vm.ConfirmDiscardIfDirtyAsync();
        if (!ok) return;

        _confirmedClose = true;
        Close();
    }

    private void OnFileListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        if (vm.SelectedFile is not { IsOpenable: true } item) return;
        if (vm.OpenFileCommand.CanExecute(item))
        {
            _ = vm.OpenFileCommand.ExecuteAsync(item);
        }
    }
}

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.Services;

namespace TICSaveEditor.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameDataContext _gameData;
    private SaveFile? _subscribedModel;

    [ObservableProperty] private string? _saveDirectoryPath;
    [ObservableProperty] private SaveDirectoryViewModel? _directory;
    [ObservableProperty] private SaveFileViewModel? _openFile;
    [ObservableProperty] private SaveFileItemViewModel? _selectedFile;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    /// <summary>View-side hook: open a folder picker, return chosen path or null on cancel.</summary>
    public Func<Task<string?>>? PickFolderAsync { get; set; }

    /// <summary>View-side hook: show the game-running confirm dialog.</summary>
    public Func<Task<bool>>? ConfirmGameRunningAsync { get; set; }

    /// <summary>View-side hook: display an error message (modal).</summary>
    public Func<string, Task>? ShowErrorAsync { get; set; }

    /// <summary>View-side hook: prompt user to discard unsaved changes (Browse/Refresh/Open/Close).</summary>
    public Func<Task<bool>>? ConfirmDiscardChangesAsync { get; set; }

    /// <summary>View-side hook: ask the user for a level (1–99); null on cancel.</summary>
    public Func<Task<int?>>? AskLevelAsync { get; set; }

    /// <summary>View-side hook: show an operation result modal (label + result).</summary>
    public Func<string, OperationResult, Task>? ShowOperationResultAsync { get; set; }

    public MainWindowViewModel(GameDataContext gameData)
    {
        _gameData = gameData;
        _saveDirectoryPath = DefaultSavePathResolver.TryGetDefault();
        TryScan(_saveDirectoryPath);
    }

    public string GameDataSummary =>
        $"Game data: {_gameData.Source} ({_gameData.Language}) — " +
        $"{_gameData.Jobs.Count} jobs, {_gameData.Items.Count} items, " +
        $"{_gameData.CharacterNames.Count} character names";

    public bool IsDirty => OpenFile?.Model.IsDirty ?? false;

    public string WindowTitle => IsDirty ? "TICSaveEditor *" : "TICSaveEditor";

    [RelayCommand]
    private async Task BrowseAsync()
    {
        if (!await ConfirmDiscardIfDirtyAsync()) return;
        StatusMessage = "Browse: starting…";
        if (PickFolderAsync is null)
        {
            StatusMessage = "Browse: PickFolderAsync hook is null (UI wiring failed)";
            return;
        }
        try
        {
            StatusMessage = "Browse: opening picker…";
            var picked = await PickFolderAsync();
            if (picked is null)
            {
                StatusMessage = "Browse: no folder returned (cancelled, picker failed, or path resolution returned null)";
                return;
            }
            if (picked.Length == 0)
            {
                StatusMessage = "Browse: empty path returned from picker";
                return;
            }
            StatusMessage = $"Browse: selected {picked}";
            SaveDirectoryPath = picked;
            OpenFile = null;
            TryScan(picked);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Browse failed: {ex.GetType().Name}: {ex.Message}";
            if (ShowErrorAsync is not null)
            {
                await ShowErrorAsync($"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (string.IsNullOrEmpty(SaveDirectoryPath)) return;
        if (!await ConfirmDiscardIfDirtyAsync()) return;
        OpenFile = null;
        TryScan(SaveDirectoryPath);
    }

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenFileAsync(SaveFileItemViewModel? item)
    {
        if (item is null || !item.IsOpenable) return;

        if (!await ConfirmDiscardIfDirtyAsync()) return;

        if (Directory?.IsGameRunning == true && ConfirmGameRunningAsync is not null)
        {
            var confirmed = await ConfirmGameRunningAsync();
            if (!confirmed) return;
        }

        IsBusy = true;
        StatusMessage = $"Opening {item.FileName}…";
        try
        {
            var path = item.Path;
            var save = await Task.Run(() => SaveFileLoader.Load(path));
            OpenFile = SaveFileViewModelFactory.Create(save, _gameData);
            StatusMessage = $"Loaded {item.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load {item.FileName}";
            if (ShowErrorAsync is not null)
            {
                await ShowErrorAsync($"{ex.GetType().Name}: {ex.Message}");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (OpenFile is null) return;
        try
        {
            var model = OpenFile.Model;
            // Save runs on the UI thread on purpose: model.Save() flips IsDirty,
            // which fires PropertyChanged → SaveCommand.NotifyCanExecuteChanged().
            // CommunityToolkit's NotifyCanExecuteChanged hops into the bound Button's
            // Command property; if we did this on the threadpool (Task.Run), Avalonia
            // throws InvalidOperationException from Button.get_Command(). Atomic write
            // of ~600KB is sub-second; UI thread is fine. If Save ever gets slow
            // enough to need a thread hop, marshal INPC back to the dispatcher rather
            // than re-introducing Task.Run here.
            model.Save();
            StatusMessage = $"Saved {Path.GetFileName(model.SourcePath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.GetType().Name}";
            if (ShowErrorAsync is not null)
            {
                await ShowErrorAsync($"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }

    private bool CanSave() => IsDirty;
    private bool CanOpenFile(SaveFileItemViewModel? item)
        => item is not null && item.IsOpenable && !IsBusy;

    public async Task<bool> ConfirmDiscardIfDirtyAsync()
    {
        if (!IsDirty) return true;
        if (ConfirmDiscardChangesAsync is null) return true;
        return await ConfirmDiscardChangesAsync();
    }

    private void TryScan(string? path)
    {
        if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path))
        {
            Directory = null;
            StatusMessage = string.IsNullOrEmpty(path)
                ? "Pick a save folder to begin."
                : $"Folder not found: {path}";
            return;
        }
        try
        {
            var dir = SaveDirectory.Scan(path);
            Directory = new SaveDirectoryViewModel(dir);
            StatusMessage = $"{Directory.Files.Count} save file(s) in {path}";
        }
        catch (Exception ex)
        {
            Directory = null;
            StatusMessage = $"Scan failed: {ex.Message}";
        }
    }

    partial void OnIsBusyChanged(bool value) => OpenFileCommand.NotifyCanExecuteChanged();

    partial void OnOpenFileChanged(SaveFileViewModel? oldValue, SaveFileViewModel? newValue)
    {
        if (_subscribedModel is not null)
        {
            _subscribedModel.PropertyChanged -= OnOpenFileModelPropertyChanged;
            _subscribedModel = null;
        }
        if (newValue?.Model is { } model)
        {
            model.PropertyChanged += OnOpenFileModelPropertyChanged;
            _subscribedModel = model;
        }
        PropagateSlotFuncs(newValue);
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(WindowTitle));
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void OnOpenFileModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null or nameof(SaveFile.IsDirty))
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(WindowTitle));
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private void PropagateSlotFuncs(SaveFileViewModel? fileVm)
    {
        switch (fileVm)
        {
            case ManualSaveFileViewModel m:
                foreach (var slot in m.Slots) ApplySlotFuncs(slot);
                break;
            case ResumeWorldSaveFileViewModel r:
                ApplySlotFuncs(r.Slot);
                break;
        }
    }

    private void ApplySlotFuncs(SaveSlotViewModel slot)
    {
        slot.AskLevelAsync = AskLevelAsync;
        slot.ShowOperationResultAsync = ShowOperationResultAsync;
    }

    /// <summary>
    /// Called by MainWindow.axaml.cs after WireHooks completes, so that any open
    /// file's slot VMs pick up the dialog Funcs that weren't yet set during
    /// PropagateSlotFuncs (the wiring is done window-Opened, after construction).
    /// </summary>
    public void RewireOpenFileSlotFuncs() => PropagateSlotFuncs(OpenFile);
}

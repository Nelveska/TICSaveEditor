using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.Services;

namespace TICSaveEditor.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly GameDataContext _gameData;

    [ObservableProperty] private string? _saveDirectoryPath;
    [ObservableProperty] private SaveDirectoryViewModel? _directory;
    [ObservableProperty] private SaveFileViewModel? _openFile;
    [ObservableProperty] private SaveFileItemViewModel? _selectedFile;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    /// <summary>
    /// View-side hook: open a folder picker and return the chosen absolute path,
    /// or null if cancelled. Set by MainWindow's code-behind in
    /// OnAttachedToVisualTree (see <c>decisions_m10_view_wiring.md</c>).
    /// </summary>
    public Func<Task<string?>>? PickFolderAsync { get; set; }

    /// <summary>
    /// View-side hook: show the game-running confirm dialog. Returns true if
    /// the user picked "Open anyway", false on Cancel.
    /// </summary>
    public Func<Task<bool>>? ConfirmGameRunningAsync { get; set; }

    /// <summary>View-side hook: display an error message (modal).</summary>
    public Func<string, Task>? ShowErrorAsync { get; set; }

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

    [RelayCommand]
    private async Task BrowseAsync()
    {
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
    private void Refresh()
    {
        if (string.IsNullOrEmpty(SaveDirectoryPath)) return;
        TryScan(SaveDirectoryPath);
    }

    [RelayCommand(CanExecute = nameof(CanOpenFile))]
    private async Task OpenFileAsync(SaveFileItemViewModel? item)
    {
        if (item is null || !item.IsOpenable) return;

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

    private bool CanOpenFile(SaveFileItemViewModel? item)
        => item is not null && item.IsOpenable && !IsBusy;

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
}

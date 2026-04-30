using CommunityToolkit.Mvvm.ComponentModel;

namespace TICSaveEditor.GUI.ViewModels.Dialogs;

public partial class LevelInputDialogViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = "Set party level";
    [ObservableProperty] private string _message = "Set every populated unit to this level (1–99):";

    private int _level = 99;
    public int Level
    {
        get => _level;
        set
        {
            var clamped = value < 1 ? 1 : value > 99 ? 99 : value;
            SetProperty(ref _level, clamped);
        }
    }

    /// <summary>Set by the dialog when the user picks OK.</summary>
    [ObservableProperty] private bool _confirmed;
}

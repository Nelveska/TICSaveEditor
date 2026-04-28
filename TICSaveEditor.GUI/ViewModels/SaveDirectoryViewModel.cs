using System.Collections.ObjectModel;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

public class SaveDirectoryViewModel : ViewModelBase
{
    public SaveDirectoryViewModel(SaveDirectory model)
    {
        Model = model;
        var items = new ObservableCollection<SaveFileItemViewModel>();
        foreach (var f in model.Files)
        {
            items.Add(new SaveFileItemViewModel(f));
        }
        Files = new ReadOnlyObservableCollection<SaveFileItemViewModel>(items);
    }

    public SaveDirectory Model { get; }
    public string Path => Model.Path;
    public bool IsGameRunning => Model.IsGameRunning;
    public ReadOnlyObservableCollection<SaveFileItemViewModel> Files { get; }
}

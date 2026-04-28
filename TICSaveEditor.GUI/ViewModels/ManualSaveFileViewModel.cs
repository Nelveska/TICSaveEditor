using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

public partial class ManualSaveFileViewModel : SaveFileViewModel
{
    [ObservableProperty]
    private SaveSlotViewModel? _selectedSlot;

    public ManualSaveFileViewModel(ManualSaveFile model, GameDataContext gameData)
        : base(model)
    {
        var slotVms = new SaveSlotViewModel[ManualSaveFile.SlotCount];
        for (int i = 0; i < ManualSaveFile.SlotCount; i++)
        {
            slotVms[i] = new SaveSlotViewModel(model.Slots[i], gameData);
        }
        Slots = new ReadOnlyObservableCollection<SaveSlotViewModel>(
            new ObservableCollection<SaveSlotViewModel>(slotVms));
    }

    public ReadOnlyObservableCollection<SaveSlotViewModel> Slots { get; }
}

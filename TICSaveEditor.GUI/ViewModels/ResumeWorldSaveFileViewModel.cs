using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

public class ResumeWorldSaveFileViewModel : SaveFileViewModel
{
    public ResumeWorldSaveFileViewModel(ResumeWorldSaveFile model, GameDataContext gameData)
        : base(model)
    {
        // ResumeWorld has one SaveWork (no slot envelope). Synthesise a SaveSlot at
        // Index = -1 so the SaveSlotView renders identically. Card.Magic on a real
        // world save is non-zero, so IsEmpty stays false.
        var syntheticSlot = new SaveSlot(-1, model.SaveWork);
        Slot = new SaveSlotViewModel(syntheticSlot, gameData);
    }

    public SaveSlotViewModel Slot { get; }
}

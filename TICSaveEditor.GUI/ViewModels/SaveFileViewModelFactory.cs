using System;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

internal static class SaveFileViewModelFactory
{
    public static SaveFileViewModel Create(SaveFile model, GameDataContext gameData) => model switch
    {
        ManualSaveFile m       => new ManualSaveFileViewModel(m, gameData),
        ResumeWorldSaveFile w  => new ResumeWorldSaveFileViewModel(w, gameData),
        ResumeBattleSaveFile b => new ResumeBattleSaveFileViewModel(b),
        _ => throw new InvalidOperationException(
            $"Unhandled save kind: {model.GetType().Name}"),
    };
}

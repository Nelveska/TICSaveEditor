using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// One row in the unit list. Resolves display name + job name via
/// <see cref="GameDataContext"/> per <c>decisions_m10_unit_name_resolution.md</c>:
/// hero short-circuit → NameNo → CharaNameKey fallback → synthetic Generic-Job-Sex.
/// All read-only; bound one-way.
/// </summary>
public class UnitListItemViewModel : ViewModelBase
{
    private const byte HeroCharacterByte = 0x01;
    private const string HeroDisplayName = "Ramza";

    private readonly GameDataContext _gameData;

    public UnitListItemViewModel(UnitSaveData model, int index, GameDataContext gameData)
    {
        Model = model;
        Index = index;
        _gameData = gameData;
    }

    public UnitSaveData Model { get; }
    public int Index { get; }
    public bool IsEmpty => Model.IsEmpty;
    public bool IsNotEmpty => !Model.IsEmpty;
    public int Level => Model.Level;
    public string IndexLabel => Index.ToString();
    public string LevelLabel => Level.ToString();
    public string JobName => _gameData.GetJobName(Model.Job);

    public string Name
    {
        get
        {
            if (Model.IsEmpty) return string.Empty;
            if (Model.Character == HeroCharacterByte) return HeroDisplayName;
            if (Model.NameNo != 0) return _gameData.GetCharacterName(Model.NameNo);
            if (Model.CharaNameKey != 0) return _gameData.GetCharacterName(Model.CharaNameKey);

            // Generic fallback. Sex byte is multi-state per
            // decisions_glain_psx_formulas.md finding 3; high-bit heuristic is
            // approximate but adequate to disambiguate same-job recruits in M10.
            var sexLabel = (Model.Sex & 0x80) != 0 ? "Male" : "Female";
            return $"Generic {_gameData.GetJobName(Model.Job)} ({sexLabel})";
        }
    }
}

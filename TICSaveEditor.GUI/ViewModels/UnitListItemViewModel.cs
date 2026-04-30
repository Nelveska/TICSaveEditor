using System;
using System.ComponentModel;
using System.Text;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// One row in the unit list. Resolves display name + job name via
/// <see cref="GameDataContext"/>; cascade is empty → hero short-circuit →
/// UnitNickname (player-set rename) → NameNo → CharaNameKey → synthetic
/// Generic-Job-Sex. See <c>decisions_chr_name_rename_storage.md</c> and
/// <c>decisions_m10_unit_name_resolution.md</c>.
///
/// Visibility is gated on <see cref="IsActive"/> (active in current party) —
/// see <c>decisions_unit_index_active_flag.md</c>. <see cref="IsEmpty"/> and
/// <see cref="IsNotEmpty"/> retain their slot-data semantics for any caller
/// that needs them.
/// </summary>
public class UnitListItemViewModel : ViewModelBase
{
    private const byte HeroCharacterByte = 0x01;
    private const string HeroDisplayName = "Ramza";

    /// <summary>Length of the UnitNickname sub-field (offset 0xDC..0xEB) inside ChrNameRaw.</summary>
    private const int UnitNicknameLength = 16;

    private readonly GameDataContext _gameData;

    public UnitListItemViewModel(UnitSaveData model, int index, GameDataContext gameData)
    {
        Model = model;
        Index = index;
        _gameData = gameData;
        Model.PropertyChanged += OnModelPropertyChanged;
    }

    public UnitSaveData Model { get; }
    public int Index { get; }
    public bool IsEmpty => Model.IsEmpty;
    public bool IsNotEmpty => !Model.IsEmpty;
    public bool IsActive => Model.IsInActiveParty(Index);
    public bool IsInactive => !IsActive;
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

            // UnitNickname[16] at offset 0xDC of the unit record (first 16 bytes
            // of ChrNameRaw). Stores the player-set rename string when present;
            // ASCII null-terminated. The remaining 48 bytes of ChrNameRaw are
            // CustomJobName[16] + field_FC[32] (separate concerns).
            var raw = Model.ChrNameRaw;
            if (raw[0] != 0)
            {
                var nullIdx = Array.IndexOf(raw, (byte)0, 0, UnitNicknameLength);
                var len = nullIdx < 0 ? UnitNicknameLength : nullIdx;
                return Encoding.ASCII.GetString(raw, 0, len);
            }

            if (Model.NameNo != 0) return _gameData.GetCharacterName(Model.NameNo);
            if (Model.CharaNameKey != 0) return _gameData.GetCharacterName(Model.CharaNameKey);

            // Generic fallback. Sex byte is multi-state per
            // decisions_glain_psx_formulas.md finding 3; high-bit heuristic is
            // approximate but adequate to disambiguate same-job recruits.
            var sexLabel = (Model.Sex & 0x80) != 0 ? "Male" : "Female";
            return $"Generic {_gameData.GetJobName(Model.Job)} ({sexLabel})";
        }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Bulk RestoreFromSnapshot fires PropertyChanged with PropertyName=null.
        // Re-raise everything in that case.
        if (e.PropertyName is null)
        {
            OnPropertyChanged((string?)null);
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(UnitSaveData.Level):
                OnPropertyChanged(nameof(Level));
                OnPropertyChanged(nameof(LevelLabel));
                break;
            case nameof(UnitSaveData.Job):
                OnPropertyChanged(nameof(JobName));
                OnPropertyChanged(nameof(Name));
                break;
            case nameof(UnitSaveData.Character):
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(IsInactive));
                break;
            case nameof(UnitSaveData.Resist):
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(IsInactive));
                break;
            case nameof(UnitSaveData.ChrNameRaw):
            case nameof(UnitSaveData.NameNo):
            case nameof(UnitSaveData.CharaNameKey):
            case nameof(UnitSaveData.Sex):
                OnPropertyChanged(nameof(Name));
                break;
            case nameof(UnitSaveData.IsEmpty):
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(IsNotEmpty));
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(IsInactive));
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(JobName));
                OnPropertyChanged(nameof(LevelLabel));
                break;
        }
    }
}

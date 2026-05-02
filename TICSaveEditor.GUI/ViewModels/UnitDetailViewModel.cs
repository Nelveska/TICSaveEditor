using System.ComponentModel;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// Composite per-unit detail VM. Hosts the active/inline editor
/// (<see cref="Live"/>) as Tab 0 and the CombatSet preset editor
/// (<see cref="CombatSets"/>) as Tab 1; <see cref="Views.UnitDetailView"/>
/// presents both inside a TabControl.
///
/// Both child VMs are constructed eagerly (cheap) and reference-stable across
/// the parent's lifetime — <see cref="SaveSlotViewModel"/> caches the
/// UnitDetailViewModel keyed on the unit model, so re-selections preserve
/// any pending edits in either tab.
///
/// Dirty-marking: <see cref="CombatSetEditorViewModel"/> already handles its
/// own MarkDirty subscription. We add a second subscription on
/// <see cref="LiveEditorViewModel"/> to mark dirty on inline edits. The Core
/// inline accessors are idempotent so spurious marks don't fire on same-value
/// writes. See <c>decisions_unit_detail_tabcontrol.md</c>.
/// </summary>
public class UnitDetailViewModel : ViewModelBase
{
    private readonly UnitListItemViewModel _unitVm;
    private readonly SaveFile? _parentFile;

    public UnitDetailViewModel(UnitListItemViewModel unitVm, GameDataContext gameData, SaveFile? parentFile)
    {
        _unitVm = unitVm;
        _parentFile = parentFile;

        Live = new LiveEditorViewModel(unitVm.Model, gameData);
        CombatSets = new CombatSetEditorViewModel(unitVm, gameData, parentFile);

        Live.PropertyChanged += OnLivePropertyChanged;
        _unitVm.PropertyChanged += OnUnitVmPropertyChanged;
    }

    public LiveEditorViewModel Live { get; }
    public CombatSetEditorViewModel CombatSets { get; }

    /// <summary>
    /// Display name for the detail header, sourced from the
    /// <see cref="UnitListItemViewModel.Name"/> cascade (Hero short-circuit →
    /// UnitNickname → NameNo → CharaNameKey → synthetic generic).
    /// </summary>
    public string UnitDisplayName => _unitVm.Name;

    private void OnLivePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Any user-driven inline mutation marks the parent file dirty. Core
        // inline setters are idempotent (same-value writes suppress INPC), so
        // MarkDirty is only called on actual changes.
        _parentFile?.MarkDirty();
    }

    private void OnUnitVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UnitListItemViewModel.Name) || e.PropertyName is null)
            OnPropertyChanged(nameof(UnitDisplayName));
    }
}

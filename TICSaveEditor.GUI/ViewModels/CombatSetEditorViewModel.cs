using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// Top-level VM for the per-unit CombatSet editor surface. Hosts the three
/// preset entry VMs and a header label for the owning unit. Subscribes to each
/// preset's <see cref="INotifyPropertyChanged"/> to call
/// <see cref="SaveFile.MarkDirty"/> on any user-driven mutation.
///
/// Live-edit semantics: changes propagate immediately through the typed
/// <see cref="CombatSet"/> setters (no Apply/Cancel). Dirty-marking is gated
/// by the typed setters' idempotent contract (same-value writes don't fire
/// PropertyChanged, so MarkDirty isn't invoked spuriously). See
/// <c>decisions_combatset_editor_ui.md</c>.
///
/// Active-preset selector (<see cref="UnitSaveData.CurrentCombatSet"/>) is
/// intentionally NOT exposed in v0.1: changing the byte without inline
/// mirroring (CLAUDE.md:126 deferred) leaves the unit's actual battle stats
/// pinned to the inline fields' OLD values, so the toggle would appear inert.
/// Editor + inline-mirror feature deferred to v0.2.
/// </summary>
public class CombatSetEditorViewModel : ViewModelBase
{
    private readonly UnitListItemViewModel _unitVm;
    private readonly UnitSaveData _unit;
    private readonly SaveFile? _parentFile;

    public CombatSetEditorViewModel(UnitListItemViewModel unitVm, GameDataContext gameData, SaveFile? parentFile)
    {
        _unitVm = unitVm;
        _unit = unitVm.Model;
        _parentFile = parentFile;

        var entries = new List<CombatSetEntryViewModel>(_unit.CombatSets.Count);
        foreach (var preset in _unit.CombatSets)
        {
            var entry = new CombatSetEntryViewModel(preset, gameData);
            entry.PropertyChanged += OnPresetChanged;
            entries.Add(entry);
        }
        Presets = new ReadOnlyObservableCollection<CombatSetEntryViewModel>(
            new ObservableCollection<CombatSetEntryViewModel>(entries));

        _unitVm.PropertyChanged += OnUnitVmPropertyChanged;
    }

    public UnitSaveData Unit => _unit;

    /// <summary>
    /// Display name for the editor header, sourced from the parent
    /// <see cref="UnitListItemViewModel.Name"/> cascade (Hero short-circuit →
    /// UnitNickname → NameNo → CharaNameKey → synthetic generic).
    /// </summary>
    public string UnitName => _unitVm.Name;
    public ReadOnlyObservableCollection<CombatSetEntryViewModel> Presets { get; }

    private void OnPresetChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Any user-driven preset mutation marks the parent file dirty. The Core
        // CombatSet setters are idempotent (same-value writes suppress INPC), so
        // MarkDirty is only called on actual changes.
        _parentFile?.MarkDirty();
    }

    private void OnUnitVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UnitListItemViewModel.Name) || e.PropertyName is null)
            OnPropertyChanged(nameof(UnitName));
    }
}

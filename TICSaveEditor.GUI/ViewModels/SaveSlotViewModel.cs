using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// Wraps a single <see cref="SaveSlot"/> for display + per-slot bulk operations.
/// ResumeWorld synthesises a <see cref="SaveSlot"/> with <see cref="SaveSlot.Index"/> = -1
/// to reuse this VM. Bulk-op buttons live here because the op target is
/// <see cref="SaveWork"/> (one slot's payload) — see decisions_m11_per_slot_op_buttons.md.
/// </summary>
public partial class SaveSlotViewModel : ViewModelBase
{
    private readonly SaveFile? _parentFile;
    private readonly GameDataContext _gameData;
    private readonly Dictionary<UnitSaveData, UnitDetailViewModel> _detailCache = new();

    /// <summary>
    /// Battle-section unit indices that contribute to <see cref="HeroNames"/>:
    /// Ramza is fixed at slot 0 (always shown if present); guest characters
    /// occupy slots 50..53 (max four concurrent guests, per the user's domain
    /// knowledge). Filtering is by <see cref="BattleSection.IsActive(int)"/>
    /// — inactive guests (UnitIndex == 0xFF) are excluded.
    /// </summary>
    private static readonly int[] HeroSlotIndices = { 0, 50, 51, 52, 53 };

    public SaveSlotViewModel(SaveSlot model, GameDataContext gameData, SaveFile? parentFile = null)
    {
        Model = model;
        _parentFile = parentFile;
        _gameData = gameData;
        var unitVms = new UnitListItemViewModel[BattleSection.UnitCount];
        for (int i = 0; i < BattleSection.UnitCount; i++)
        {
            unitVms[i] = new UnitListItemViewModel(model.SaveWork.Battle.Units[i], i, gameData);
        }
        Units = new ReadOnlyObservableCollection<UnitListItemViewModel>(
            new ObservableCollection<UnitListItemViewModel>(unitVms));

        // Re-raise HeroNames when Ramza or any guest changes name or active status.
        foreach (var idx in HeroSlotIndices)
            Units[idx].PropertyChanged += OnHeroUnitChanged;
    }

    private void OnHeroUnitChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UnitListItemViewModel.Name)
            || e.PropertyName == nameof(UnitListItemViewModel.IsActive))
        {
            OnPropertyChanged(nameof(HeroNames));
        }
    }

    public SaveSlot Model { get; }

    public int Index => Model.Index;
    public bool IsEmpty => Model.IsEmpty;
    public bool IsNotEmpty => !Model.IsEmpty;
    public string SlotLabel => Index < 0 ? "Resume" : $"Slot {Index}";

    public string TitleDisplay
    {
        get
        {
            if (Model.IsEmpty) return "—";
            var t = Model.SlotTitle;
            return string.IsNullOrEmpty(t) ? "—" : t;
        }
    }

    /// <summary>
    /// View-side hook: ask the user for an integer level (1–99) via a modal.
    /// Returns null if the user cancels. Set by <c>MainWindow.axaml.cs</c> and
    /// propagated through the parent file VM after each slot is constructed.
    /// </summary>
    public Func<Task<int?>>? AskLevelAsync { get; set; }

    /// <summary>
    /// View-side hook: show an OperationResult modal with the op label as title.
    /// Set by <c>MainWindow.axaml.cs</c> via the parent file VM.
    /// </summary>
    public Func<string, OperationResult, Task>? ShowOperationResultAsync { get; set; }

    public string SaveTimestampDisplay
    {
        get
        {
            // Card.SaveTimestamp returns DateTimeKind.Utc per CardSection.cs:52.
            // Slots with Magic != 0 but Unix-epoch-0 timestamps render as "—"
            // (the user reported "1969-12-31 15:59" rows in their live save —
            // those are slots whose IsEmpty heuristic failed; rendering them
            // as "—" is honest about not knowing the real save date).
            var ts = Model.SaveTimestamp;
            if (ts == DateTime.UnixEpoch) return "—";
            var local = DateTime.SpecifyKind(ts, DateTimeKind.Utc).ToLocalTime();
            return local.ToString("yyyy-MM-dd HH:mm");
        }
    }

    public string PlaytimeDisplay
    {
        get
        {
            var pt = Model.Playtime;
            return $"{(int)pt.TotalHours}h {pt.Minutes:D2}m";
        }
    }

    /// <summary>
    /// Comma-joined list of "story characters in the active party": always
    /// <c>Units[0]</c> (Ramza) when present, plus any of <c>Units[50..53]</c>
    /// whose <see cref="UnitListItemViewModel.IsActive"/> is true (guest
    /// slots; departed guests have UnitIndex == 0xFF and are excluded).
    /// Mirrors the in-game save-list display, which never shows generic
    /// recruits. Returns "—" for empty save slots.
    /// </summary>
    public string HeroNames
    {
        get
        {
            if (Model.IsEmpty) return "—";
            var names = new List<string>();
            if (!Units[0].IsEmpty)
                names.Add(Units[0].Name);
            for (int i = 50; i <= 53; i++)
                if (Units[i].IsActive)
                    names.Add(Units[i].Name);
            return names.Count == 0 ? "—" : string.Join(", ", names);
        }
    }

    public ReadOnlyObservableCollection<UnitListItemViewModel> Units { get; }

    /// <summary>
    /// Currently selected unit row, two-way bound from <see cref="Views.UnitListView"/>.
    /// Drives the per-unit detail panel (Live + Combat Sets tabs); a null
    /// selection collapses the right panel. See
    /// <c>decisions_unit_detail_tabcontrol.md</c>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedUnitDetail))]
    private UnitListItemViewModel? _selectedUnit;

    /// <summary>
    /// Lazy-cached <see cref="UnitDetailViewModel"/> (composite Live + Combat
    /// Sets) for the currently selected unit. Returns null when no unit is
    /// selected or the unit's model is empty. Reference equality is preserved
    /// across selection cycles — re-selecting the same unit returns the cached
    /// VM with any pending edits in either tab intact.
    /// </summary>
    public UnitDetailViewModel? SelectedUnitDetail
    {
        get
        {
            var unit = SelectedUnit;
            if (unit is null || unit.IsEmpty) return null;
            var model = unit.Model;
            if (!_detailCache.TryGetValue(model, out var detail))
            {
                detail = new UnitDetailViewModel(unit, _gameData, _parentFile);
                _detailCache[model] = detail;
            }
            return detail;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunBulkOp))]
    private async Task SetAllToLevelAsync()
    {
        if (AskLevelAsync is null) return;
        var level = await AskLevelAsync();
        if (level is null) return;
        var result = PartyOperations.SetAllToLevel(Model.SaveWork, level.Value);
        if (result.Succeeded && result.UnitsAffected > 0) _parentFile?.MarkDirty();
        if (ShowOperationResultAsync is not null)
            await ShowOperationResultAsync("Set all to level", result);
    }

    [RelayCommand(CanExecute = nameof(CanRunBulkOp))]
    private async Task MaxAllJobPointsAsync()
    {
        var result = PartyOperations.MaxAllJobPoints(Model.SaveWork);
        if (result.Succeeded && result.UnitsAffected > 0) _parentFile?.MarkDirty();
        if (ShowOperationResultAsync is not null)
            await ShowOperationResultAsync("Max all job points", result);
    }

    [RelayCommand(CanExecute = nameof(CanRunBulkOp))]
    private async Task LearnAllAbilitiesCurrentJobAsync()
    {
        var result = PartyOperations.LearnAllAbilitiesCurrentJob(Model.SaveWork);
        if (result.Succeeded && result.UnitsAffected > 0) _parentFile?.MarkDirty();
        if (ShowOperationResultAsync is not null)
            await ShowOperationResultAsync("Learn all abilities (current job)", result);
    }

    private bool CanRunBulkOp() => !IsEmpty;
}

using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Operations;
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

    public SaveSlotViewModel(SaveSlot model, GameDataContext gameData, SaveFile? parentFile = null)
    {
        Model = model;
        _parentFile = parentFile;
        var unitVms = new UnitListItemViewModel[BattleSection.UnitCount];
        for (int i = 0; i < BattleSection.UnitCount; i++)
        {
            unitVms[i] = new UnitListItemViewModel(model.SaveWork.Battle.Units[i], i, gameData);
        }
        Units = new ReadOnlyObservableCollection<UnitListItemViewModel>(
            new ObservableCollection<UnitListItemViewModel>(unitVms));
    }

    public SaveSlot Model { get; }

    public int Index => Model.Index;
    public bool IsEmpty => Model.IsEmpty;
    public bool IsNotEmpty => !Model.IsEmpty;
    public string SlotLabel => Index < 0 ? "Resume" : $"Slot {Index}";
    public string Title => Model.SlotTitle;

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
            // Playtime in TIC manual saves at InfoSection offset 0x74 reads 0
            // for valid populated saves with correct timestamps. Real bug —
            // offset is wrong or playtime is stored elsewhere. Render "—" until
            // a SaveDiff fixture pinpoints the correct offset (deferred to v0.2).
            var pt = Model.Playtime;
            if (pt == TimeSpan.Zero) return "—";
            return $"{(int)pt.TotalHours}h {pt.Minutes:D2}m";
        }
    }

    public string HeroName
    {
        get
        {
            // TIC manual saves zero-fill InfoSection.HeroNameRaw — names are
            // resolved via name_no + locale lookup (CLAUDE.md gotcha). Raw ASCII
            // names only appear in battle-format files, which v0.1 doesn't edit.
            // Display "—" so the empty bytes don't masquerade as missing data.
            // Real fix: resolve via NameNo lookup at viewmodel layer (v0.2+).
            var raw = Model.HeroNameRaw;
            if (Array.TrueForAll(raw, b => b == 0)) return "—";
            var nullIdx = Array.IndexOf(raw, (byte)0);
            var len = nullIdx < 0 ? raw.Length : nullIdx;
            return Encoding.ASCII.GetString(raw, 0, len);
        }
    }

    public ReadOnlyObservableCollection<UnitListItemViewModel> Units { get; }

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

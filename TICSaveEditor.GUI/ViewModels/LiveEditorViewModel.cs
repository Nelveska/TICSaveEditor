using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// View model for a single unit's INLINE / active loadout state — distinct from
/// the three <see cref="CombatSet"/> presets (those are wrapped by
/// <see cref="CombatSetEntryViewModel"/>). Mirrors the CombatSet editor's
/// surface (Job + Secondary skillset + 3 abilities + 5 equipment slots) but
/// binds to the inline <see cref="UnitSaveData"/> fields and adds a read-only
/// "Primary" display derived from the active job.
///
/// Equipment shape: 5 logical slots in the UI (Rh / Lh / Head / Armor /
/// Accessory) — matches the player mental model and the CombatSet preset
/// shape — even though inline storage is 7 raw u16 fields (Head/Body/Accessory
/// + per-hand RHWeapon/RHShield/LHWeapon/LHShield). Hand slots demux on
/// write based on <see cref="ItemInfo.ItemCategory"/>; on read, prefer the
/// non-empty field (Shield wins on the anomalous both-non-empty collision).
/// See <c>decisions_inline_equip_5slot_demux.md</c>.
///
/// Primary skillset is read-only in v0.1 — the inline UnitData struct has no
/// dedicated primary-skillset field; the engine derives it from the active
/// Job at runtime. Future "override primary" toggle deferred to v0.2 (some
/// mods give units non-default primary skillsets; storage location TBD).
/// </summary>
public class LiveEditorViewModel : ViewModelBase
{
    private const int RhWeaponSlot = 3;
    private const int RhShieldSlot = 4;
    private const int LhWeaponSlot = 5;
    private const int LhShieldSlot = 6;
    private const int HeadSlot = 0;
    private const int BodySlot = 1;
    private const int AccessorySlot = 2;
    private const string ShieldCategory = "Shield";

    private readonly UnitSaveData _model;
    private readonly GameDataContext _gameData;

    public LiveEditorViewModel(UnitSaveData model, GameDataContext gameData)
    {
        _model = model;
        _gameData = gameData;

        JobOptions = EquipmentLoadoutHelpers.BuildJobOptions(model.Job, gameData);
        SecondaryOptions = EquipmentLoadoutHelpers.BuildSecondarySkillsetOptions(model.SecondaryAction, gameData);
        ReactionOptions = EquipmentLoadoutHelpers.BuildAbilityOptionsByType(model.ReactionAbility, gameData, EquipmentLoadoutHelpers.ReactionAbilityType);
        SupportOptions = EquipmentLoadoutHelpers.BuildAbilityOptionsByType(model.SupportAbility, gameData, EquipmentLoadoutHelpers.SupportAbilityType);
        MovementOptions = EquipmentLoadoutHelpers.BuildAbilityOptionsByType(model.MovementAbility, gameData, EquipmentLoadoutHelpers.MovementAbilityType);
        RhOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(GetCurrentRh(), gameData, EquipmentLoadoutHelpers.RhLhCategories);
        LhOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(GetCurrentLh(), gameData, EquipmentLoadoutHelpers.RhLhCategories);
        HeadOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.GetEquipItem(HeadSlot), gameData, EquipmentLoadoutHelpers.HeadCategories);
        ArmorOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.GetEquipItem(BodySlot), gameData, EquipmentLoadoutHelpers.ArmorCategories);
        AccessoryOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.GetEquipItem(AccessorySlot), gameData, EquipmentLoadoutHelpers.AccessoryCategories);

        _model.PropertyChanged += OnModelPropertyChanged;

        // Equipment changes don't fire UnitSaveData.PropertyChanged for the
        // "EquipItems" collection name on individual mutations — that event
        // only fires inside the SuspendNotifications scope's Dispose path
        // (bulk ops). On normal user edits, NotifyOrQueue raises only the
        // entry-level Value/IsEmpty INPC. Subscribe per-entry so Live's
        // SelectedX item INPC re-fires on every UI-driven write, which lets
        // UnitDetailViewModel mark the parent file dirty.
        foreach (var entry in _model.EquipItems)
            entry.PropertyChanged += OnEquipItemEntryChanged;
    }

    private void OnEquipItemEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-raise all 5 SelectedX item INPC events. Per-entry resolution
        // would be nominally cheaper but Rh/Lh demux writes touch two entries
        // per change anyway, so coalescing isn't a meaningful win and the
        // simpler all-fire is safer (idempotent at the MarkDirty consumer).
        OnPropertyChanged(nameof(SelectedRh));
        OnPropertyChanged(nameof(SelectedLh));
        OnPropertyChanged(nameof(SelectedHead));
        OnPropertyChanged(nameof(SelectedArmor));
        OnPropertyChanged(nameof(SelectedAccessory));
    }

    public UnitSaveData Model => _model;

    public IReadOnlyList<JobInfo> JobOptions { get; }
    public JobInfo? SelectedJob
    {
        get => JobOptions.FirstOrDefault(j => j.Id == _model.Job);
        set { if (value is not null) _model.Job = (byte)value.Id; }
    }

    /// <summary>
    /// Read-only display of the primary skillset implied by the active Job.
    /// The inline UnitData has no dedicated primary-skillset field; the engine
    /// derives it from <see cref="JobInfo.JobCommandId"/>. Renders "(unknown)"
    /// when the Job lookup fails.
    /// </summary>
    // TODO(v0.2): expose Override Primary toggle once mod use case is concrete.
    public string PrimarySkillsetDisplay
    {
        get
        {
            if (_gameData.TryGetJob(_model.Job, out var jobInfo) && jobInfo is not null)
            {
                var name = _gameData.GetCommandName(jobInfo.JobCommandId);
                return string.IsNullOrEmpty(name) ? "(unknown)" : name;
            }
            return "(unknown)";
        }
    }

    public IReadOnlyList<JobCommandInfo> SecondaryOptions { get; }
    public JobCommandInfo? SelectedSecondary
    {
        get => SecondaryOptions.FirstOrDefault(c => c.Id == _model.SecondaryAction);
        set { if (value is not null) _model.SecondaryAction = (byte)value.Id; }
    }

    public IReadOnlyList<AbilityInfo> ReactionOptions { get; }
    public AbilityInfo? SelectedReaction
    {
        get => ReactionOptions.FirstOrDefault(a => a.Id == _model.ReactionAbility);
        set { if (value is not null) _model.ReactionAbility = (ushort)value.Id; }
    }

    public IReadOnlyList<AbilityInfo> SupportOptions { get; }
    public AbilityInfo? SelectedSupport
    {
        get => SupportOptions.FirstOrDefault(a => a.Id == _model.SupportAbility);
        set { if (value is not null) _model.SupportAbility = (ushort)value.Id; }
    }

    public IReadOnlyList<AbilityInfo> MovementOptions { get; }
    public AbilityInfo? SelectedMovement
    {
        get => MovementOptions.FirstOrDefault(a => a.Id == _model.MovementAbility);
        set { if (value is not null) _model.MovementAbility = (ushort)value.Id; }
    }

    public IReadOnlyList<ItemInfo> RhOptions { get; }
    public ItemInfo? SelectedRh
    {
        get => RhOptions.FirstOrDefault(i => i.Id == GetCurrentRh());
        set { if (value is not null) DemuxAndWriteHand(RhWeaponSlot, RhShieldSlot, (ushort)value.Id); }
    }

    public IReadOnlyList<ItemInfo> LhOptions { get; }
    public ItemInfo? SelectedLh
    {
        get => LhOptions.FirstOrDefault(i => i.Id == GetCurrentLh());
        set { if (value is not null) DemuxAndWriteHand(LhWeaponSlot, LhShieldSlot, (ushort)value.Id); }
    }

    public IReadOnlyList<ItemInfo> HeadOptions { get; }
    public ItemInfo? SelectedHead
    {
        get => HeadOptions.FirstOrDefault(i => i.Id == _model.GetEquipItem(HeadSlot));
        set { if (value is not null) _model.SetEquipItem(HeadSlot, (ushort)value.Id); }
    }

    public IReadOnlyList<ItemInfo> ArmorOptions { get; }
    public ItemInfo? SelectedArmor
    {
        get => ArmorOptions.FirstOrDefault(i => i.Id == _model.GetEquipItem(BodySlot));
        set { if (value is not null) _model.SetEquipItem(BodySlot, (ushort)value.Id); }
    }

    public IReadOnlyList<ItemInfo> AccessoryOptions { get; }
    public ItemInfo? SelectedAccessory
    {
        get => AccessoryOptions.FirstOrDefault(i => i.Id == _model.GetEquipItem(AccessorySlot));
        set { if (value is not null) _model.SetEquipItem(AccessorySlot, (ushort)value.Id); }
    }

    private ushort GetCurrentRh()
    {
        var shield = _model.GetEquipItem(RhShieldSlot);
        var weapon = _model.GetEquipItem(RhWeaponSlot);
        return shield != UnitSaveData.EmptyEquipSlotSentinel ? shield : weapon;
    }

    private ushort GetCurrentLh()
    {
        var shield = _model.GetEquipItem(LhShieldSlot);
        var weapon = _model.GetEquipItem(LhWeaponSlot);
        return shield != UnitSaveData.EmptyEquipSlotSentinel ? shield : weapon;
    }

    /// <summary>
    /// Demux a hand-slot write into the inline 7-slot raw form. If the chosen
    /// item is the Empty sentinel, both weapon and shield fields are cleared.
    /// Otherwise the item's <see cref="ItemInfo.ItemCategory"/> selects which
    /// field (Shield → shield slot, anything else → weapon slot); the other
    /// field is cleared so the engine sees a single occupant per hand.
    /// </summary>
    private void DemuxAndWriteHand(int weaponSlot, int shieldSlot, ushort id)
    {
        if (id == UnitSaveData.EmptyEquipSlotSentinel)
        {
            _model.SetEquipItem(weaponSlot, UnitSaveData.EmptyEquipSlotSentinel);
            _model.SetEquipItem(shieldSlot, UnitSaveData.EmptyEquipSlotSentinel);
            return;
        }

        bool isShield = _gameData.TryGetItem(id, out var info)
            && info is not null
            && info.ItemCategory == ShieldCategory;

        if (isShield)
        {
            _model.SetEquipItem(shieldSlot, id);
            _model.SetEquipItem(weaponSlot, UnitSaveData.EmptyEquipSlotSentinel);
        }
        else
        {
            _model.SetEquipItem(weaponSlot, id);
            _model.SetEquipItem(shieldSlot, UnitSaveData.EmptyEquipSlotSentinel);
        }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(UnitSaveData.Job):
                OnPropertyChanged(nameof(SelectedJob));
                OnPropertyChanged(nameof(PrimarySkillsetDisplay));
                break;
            case nameof(UnitSaveData.SecondaryAction): OnPropertyChanged(nameof(SelectedSecondary)); break;
            case nameof(UnitSaveData.ReactionAbility): OnPropertyChanged(nameof(SelectedReaction)); break;
            case nameof(UnitSaveData.SupportAbility): OnPropertyChanged(nameof(SelectedSupport)); break;
            case nameof(UnitSaveData.MovementAbility): OnPropertyChanged(nameof(SelectedMovement)); break;
            case nameof(UnitSaveData.EquipItems):
                OnPropertyChanged(nameof(SelectedRh));
                OnPropertyChanged(nameof(SelectedLh));
                OnPropertyChanged(nameof(SelectedHead));
                OnPropertyChanged(nameof(SelectedArmor));
                OnPropertyChanged(nameof(SelectedAccessory));
                break;
            case null:
                // Bulk re-raise on snapshot rehydrate (CLAUDE.md M8 pattern).
                OnPropertyChanged((string?)null);
                break;
        }
    }
}

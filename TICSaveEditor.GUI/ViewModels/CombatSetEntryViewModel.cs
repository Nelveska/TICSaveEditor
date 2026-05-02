using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.GUI.ViewModels;

/// <summary>
/// View model for one of a unit's three <see cref="CombatSet"/> presets. Wraps
/// the typed accessors (Name, Job, Skillset0/1, Reaction/Support/MovementAbility,
/// Rh/Lh/Head/Armor/Accessory) with ComboBox-friendly Selected* properties backed
/// by per-entry option lists.
///
/// Option-list filtering, the per-slot ItemCategory permitted-sets, and the
/// synthetic Empty/Unknown ComboBox plumbing all live in
/// <see cref="EquipmentLoadoutHelpers"/> (extracted 2026-05-01 for shared reuse
/// with the inline Live editor). See <c>decisions_combatset_editor_ui.md</c>,
/// <c>decisions_combatset_item_accessors.md</c>, and
/// <c>decisions_inline_equip_5slot_demux.md</c>.
///
/// IsDoubleHand intentionally NOT exposed in v0.1: byte at section-relative 0x57
/// is speculative (Nenkai's i16 Job spans 0x56..0x57; no DoubleHand toggle SaveDiff
/// fixture exists), and toggling on a unit lacking the DoubleHand support/innate
/// has unknown safety. Core <c>CombatSet.IsDoubleHand</c> retained for byte-faithful
/// round-trip but no UI consumer.
/// </summary>
public class CombatSetEntryViewModel : ViewModelBase
{
    private readonly CombatSet _model;

    public CombatSetEntryViewModel(CombatSet model, GameDataContext gameData)
    {
        _model = model;

        JobOptions = EquipmentLoadoutHelpers.BuildJobOptions(model.Job, gameData);
        SkillsetOptions = EquipmentLoadoutHelpers.BuildSkillsetOptions(model.Skillset0, model.Skillset1, gameData);
        ReactionOptions = EquipmentLoadoutHelpers.BuildAbilityOptionsByType(model.ReactionAbility, gameData, EquipmentLoadoutHelpers.ReactionAbilityType);
        SupportOptions = EquipmentLoadoutHelpers.BuildAbilityOptionsByType(model.SupportAbility, gameData, EquipmentLoadoutHelpers.SupportAbilityType);
        MovementOptions = EquipmentLoadoutHelpers.BuildAbilityOptionsByType(model.MovementAbility, gameData, EquipmentLoadoutHelpers.MovementAbilityType);
        RhOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.Rh, gameData, EquipmentLoadoutHelpers.RhLhCategories);
        LhOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.Lh, gameData, EquipmentLoadoutHelpers.RhLhCategories);
        HeadOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.Head, gameData, EquipmentLoadoutHelpers.HeadCategories);
        ArmorOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.Armor, gameData, EquipmentLoadoutHelpers.ArmorCategories);
        AccessoryOptions = EquipmentLoadoutHelpers.BuildItemOptionsBySlot(model.Accessory, gameData, EquipmentLoadoutHelpers.AccessoryCategories);

        _model.PropertyChanged += OnModelPropertyChanged;
    }

    public CombatSet Model => _model;
    public int Index => _model.Index;
    public string Header => $"Set {_model.Index}";

    public string Name
    {
        get => _model.Name;
        set => _model.Name = value;
    }

    public IReadOnlyList<JobInfo> JobOptions { get; }
    public JobInfo? SelectedJob
    {
        get => JobOptions.FirstOrDefault(j => j.Id == _model.Job);
        set { if (value is not null) _model.Job = (byte)value.Id; }
    }

    public IReadOnlyList<JobCommandInfo> SkillsetOptions { get; }
    public JobCommandInfo? SelectedSkillset0
    {
        get => SkillsetOptions.FirstOrDefault(c => c.Id == _model.Skillset0);
        set { if (value is not null) _model.Skillset0 = (short)value.Id; }
    }
    public JobCommandInfo? SelectedSkillset1
    {
        get => SkillsetOptions.FirstOrDefault(c => c.Id == _model.Skillset1);
        set { if (value is not null) _model.Skillset1 = (short)value.Id; }
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
        get => RhOptions.FirstOrDefault(i => i.Id == _model.Rh);
        set { if (value is not null) _model.Rh = (ushort)value.Id; }
    }

    public IReadOnlyList<ItemInfo> LhOptions { get; }
    public ItemInfo? SelectedLh
    {
        get => LhOptions.FirstOrDefault(i => i.Id == _model.Lh);
        set { if (value is not null) _model.Lh = (ushort)value.Id; }
    }

    public IReadOnlyList<ItemInfo> HeadOptions { get; }
    public ItemInfo? SelectedHead
    {
        get => HeadOptions.FirstOrDefault(i => i.Id == _model.Head);
        set { if (value is not null) _model.Head = (ushort)value.Id; }
    }

    public IReadOnlyList<ItemInfo> ArmorOptions { get; }
    public ItemInfo? SelectedArmor
    {
        get => ArmorOptions.FirstOrDefault(i => i.Id == _model.Armor);
        set { if (value is not null) _model.Armor = (ushort)value.Id; }
    }

    public IReadOnlyList<ItemInfo> AccessoryOptions { get; }
    public ItemInfo? SelectedAccessory
    {
        get => AccessoryOptions.FirstOrDefault(i => i.Id == _model.Accessory);
        set { if (value is not null) _model.Accessory = (ushort)value.Id; }
    }

    private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CombatSet.Name): OnPropertyChanged(nameof(Name)); break;
            case nameof(CombatSet.Job): OnPropertyChanged(nameof(SelectedJob)); break;
            case nameof(CombatSet.Skillset0): OnPropertyChanged(nameof(SelectedSkillset0)); break;
            case nameof(CombatSet.Skillset1): OnPropertyChanged(nameof(SelectedSkillset1)); break;
            case nameof(CombatSet.ReactionAbility): OnPropertyChanged(nameof(SelectedReaction)); break;
            case nameof(CombatSet.SupportAbility): OnPropertyChanged(nameof(SelectedSupport)); break;
            case nameof(CombatSet.MovementAbility): OnPropertyChanged(nameof(SelectedMovement)); break;
            case nameof(CombatSet.Rh): OnPropertyChanged(nameof(SelectedRh)); break;
            case nameof(CombatSet.Lh): OnPropertyChanged(nameof(SelectedLh)); break;
            case nameof(CombatSet.Head): OnPropertyChanged(nameof(SelectedHead)); break;
            case nameof(CombatSet.Armor): OnPropertyChanged(nameof(SelectedArmor)); break;
            case nameof(CombatSet.Accessory): OnPropertyChanged(nameof(SelectedAccessory)); break;
            case null:
                // Bulk re-raise on snapshot rehydrate (CLAUDE.md M8 pattern).
                OnPropertyChanged((string?)null);
                break;
        }
    }
}

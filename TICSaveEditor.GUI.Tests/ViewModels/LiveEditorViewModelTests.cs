using System.ComponentModel;
using System.IO;
using System.Linq;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Exercises the inline (active) loadout editor against the Baseline real-save
/// fixture. Construction is via the SaveSlotViewModel.SelectedUnitDetail.Live
/// path — matches the production wiring (UnitListView ListBox SelectedItem →
/// SaveSlotViewModel.SelectedUnit → SelectedUnitDetail → .Live).
///
/// Coverage:
/// - 11 Selected* fields round-trip through inline UnitSaveData accessors
/// - Read-only PrimarySkillsetDisplay derived from active Job
/// - Rh/Lh hand demux: writes route to weapon vs shield slot by ItemCategory
/// - Rh/Lh read precedence: prefer non-empty (Shield wins on collision anomaly)
/// - (Empty) synthetic at index 0 of every item options list
/// - Idempotent setters do not fire spurious dirty marks
/// - Filter-miss synthetic Unknown Item inserted for current ID outside category
/// - Real-fixture mutate → save → reload preserves the demuxed write
/// </summary>
public class LiveEditorViewModelTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public LiveEditorViewModelTests(GameDataFixture fixture) => _fixture = fixture;

    private (ManualSaveFileViewModel vm, SaveSlotViewModel slot, UnitListItemViewModel unit) LoadBaselineWithPopulatedUnit()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        slot.SelectedUnit = unit;
        return (vm, slot, unit);
    }

    private LiveEditorViewModel GetLive(SaveSlotViewModel slot) => slot.SelectedUnitDetail!.Live;

    // ===== Construction + read-out =====

    [Fact]
    public void Construction_resolves_all_selected_fields()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.NotNull(live.SelectedJob);
        Assert.NotNull(live.SelectedSecondary);
        Assert.NotNull(live.SelectedReaction);
        Assert.NotNull(live.SelectedSupport);
        Assert.NotNull(live.SelectedMovement);
        Assert.NotNull(live.SelectedRh);
        Assert.NotNull(live.SelectedLh);
        Assert.NotNull(live.SelectedHead);
        Assert.NotNull(live.SelectedArmor);
        Assert.NotNull(live.SelectedAccessory);
    }

    [Fact]
    public void PrimarySkillsetDisplay_resolves_to_non_empty_for_known_job()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(string.IsNullOrEmpty(live.PrimarySkillsetDisplay));
    }

    [Fact]
    public void PrimarySkillsetDisplay_updates_when_Job_changes()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var before = live.PrimarySkillsetDisplay;

        var newJob = live.JobOptions
            .Where(j => j.Id != unit.Model.Job)
            .FirstOrDefault(j => _fixture.Context.TryGetJob(j.Id, out var info)
                && info is not null
                && info.JobCommandId != 0
                && _fixture.Context.GetCommandName(info.JobCommandId) != before);
        Assert.NotNull(newJob);
        live.SelectedJob = newJob;

        Assert.NotEqual(before, live.PrimarySkillsetDisplay);
    }

    // ===== Round-trips =====

    [Fact]
    public void SelectedJob_round_trips_through_inline_Job_byte()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.JobOptions.First(j => j.Id != unit.Model.Job);
        live.SelectedJob = target;
        Assert.Equal((byte)target.Id, unit.Model.Job);
    }

    [Fact]
    public void SelectedSecondary_round_trips_through_SecondaryAction_byte()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.SecondaryOptions.First(c => c.Id != unit.Model.SecondaryAction);
        live.SelectedSecondary = target;
        Assert.Equal((byte)target.Id, unit.Model.SecondaryAction);
    }

    [Fact]
    public void SelectedReaction_round_trips_through_ReactionAbility_ushort()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.ReactionOptions.First(a => a.Id != unit.Model.ReactionAbility);
        live.SelectedReaction = target;
        Assert.Equal((ushort)target.Id, unit.Model.ReactionAbility);
    }

    [Fact]
    public void SelectedSupport_round_trips_through_SupportAbility_ushort()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.SupportOptions.First(a => a.Id != unit.Model.SupportAbility);
        live.SelectedSupport = target;
        Assert.Equal((ushort)target.Id, unit.Model.SupportAbility);
    }

    [Fact]
    public void SelectedMovement_round_trips_through_MovementAbility_ushort()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.MovementOptions.First(a => a.Id != unit.Model.MovementAbility);
        live.SelectedMovement = target;
        Assert.Equal((ushort)target.Id, unit.Model.MovementAbility);
    }

    [Fact]
    public void SelectedHead_round_trips_via_GetEquipItem_0()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.HeadOptions
            .First(i => i.Id != unit.Model.GetEquipItem(0) && i.Id != UnitSaveData.EmptyEquipSlotSentinel);
        live.SelectedHead = target;
        Assert.Equal((ushort)target.Id, unit.Model.GetEquipItem(0));
    }

    [Fact]
    public void SelectedArmor_round_trips_via_GetEquipItem_1()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.ArmorOptions
            .First(i => i.Id != unit.Model.GetEquipItem(1) && i.Id != UnitSaveData.EmptyEquipSlotSentinel);
        live.SelectedArmor = target;
        Assert.Equal((ushort)target.Id, unit.Model.GetEquipItem(1));
    }

    [Fact]
    public void SelectedAccessory_round_trips_via_GetEquipItem_2()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var target = live.AccessoryOptions
            .First(i => i.Id != unit.Model.GetEquipItem(2) && i.Id != UnitSaveData.EmptyEquipSlotSentinel);
        live.SelectedAccessory = target;
        Assert.Equal((ushort)target.Id, unit.Model.GetEquipItem(2));
    }

    // ===== Hand demux =====

    [Fact]
    public void SelectedRh_set_to_weapon_writes_RHWeapon_clears_RHShield()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var weapon = live.RhOptions.First(i => i.ItemCategory == "Sword");
        live.SelectedRh = weapon;
        Assert.Equal((ushort)weapon.Id, unit.Model.GetEquipItem(3));
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(4));
    }

    [Fact]
    public void SelectedRh_set_to_shield_writes_RHShield_clears_RHWeapon()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var shield = live.RhOptions.First(i => i.ItemCategory == "Shield");
        live.SelectedRh = shield;
        Assert.Equal((ushort)shield.Id, unit.Model.GetEquipItem(4));
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(3));
    }

    [Fact]
    public void SelectedRh_set_to_Empty_clears_both_hand_fields()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var empty = live.RhOptions.First(i => i.Id == UnitSaveData.EmptyEquipSlotSentinel);
        live.SelectedRh = empty;
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(3));
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(4));
    }

    [Fact]
    public void SelectedRh_read_prefers_Shield_when_only_Shield_set()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var shield = _fixture.Context.Items.First(i => i.ItemCategory == "Shield");
        unit.Model.SetEquipItem(3, UnitSaveData.EmptyEquipSlotSentinel);
        unit.Model.SetEquipItem(4, (ushort)shield.Id);
        var live = GetLive(slot);
        Assert.Equal(shield.Id, live.SelectedRh!.Id);
    }

    [Fact]
    public void SelectedRh_read_returns_Weapon_when_only_Weapon_set()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var weapon = _fixture.Context.Items.First(i => i.ItemCategory == "Sword");
        unit.Model.SetEquipItem(3, (ushort)weapon.Id);
        unit.Model.SetEquipItem(4, UnitSaveData.EmptyEquipSlotSentinel);
        var live = GetLive(slot);
        Assert.Equal(weapon.Id, live.SelectedRh!.Id);
    }

    [Fact]
    public void SelectedRh_read_returns_Empty_when_both_clear()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        unit.Model.SetEquipItem(3, UnitSaveData.EmptyEquipSlotSentinel);
        unit.Model.SetEquipItem(4, UnitSaveData.EmptyEquipSlotSentinel);
        var live = GetLive(slot);
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, (ushort)live.SelectedRh!.Id);
        Assert.Equal("(Empty)", live.SelectedRh.Name);
    }

    [Fact]
    public void SelectedLh_set_to_weapon_writes_LHWeapon_clears_LHShield()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var weapon = live.LhOptions.First(i => i.ItemCategory == "Sword");
        live.SelectedLh = weapon;
        Assert.Equal((ushort)weapon.Id, unit.Model.GetEquipItem(5));
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(6));
    }

    [Fact]
    public void SelectedLh_set_to_shield_writes_LHShield_clears_LHWeapon()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var shield = live.LhOptions.First(i => i.ItemCategory == "Shield");
        live.SelectedLh = shield;
        Assert.Equal((ushort)shield.Id, unit.Model.GetEquipItem(6));
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(5));
    }

    [Fact]
    public void SelectedLh_set_to_Empty_clears_both_hand_fields()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var empty = live.LhOptions.First(i => i.Id == UnitSaveData.EmptyEquipSlotSentinel);
        live.SelectedLh = empty;
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(5));
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, unit.Model.GetEquipItem(6));
    }

    // ===== Synthetic Empty + Unknown =====

    [Theory]
    [InlineData("Rh")]
    [InlineData("Lh")]
    [InlineData("Head")]
    [InlineData("Armor")]
    [InlineData("Accessory")]
    public void Item_options_always_prepend_Empty_synthetic_at_index_0(string slotName)
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        var options = slotName switch
        {
            "Rh" => live.RhOptions,
            "Lh" => live.LhOptions,
            "Head" => live.HeadOptions,
            "Armor" => live.ArmorOptions,
            "Accessory" => live.AccessoryOptions,
            _ => throw new System.ArgumentException(slotName),
        };
        Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, (ushort)options[0].Id);
        Assert.Equal("(Empty)", options[0].Name);
    }

    [Fact]
    public void Filter_miss_synthetic_Unknown_Item_inserted_when_current_outside_category()
    {
        // Coerce Head slot to a Sword-category id (not in HeadCategories) BEFORE
        // construction so the Live VM builds with the wrong-category id current.
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        var sword = _fixture.Context.Items.First(i => i.ItemCategory == "Sword");
        unit.Model.SetEquipItem(0, (ushort)sword.Id);

        slot.SelectedUnit = unit;
        var live = GetLive(slot);
        // Index 0 is (Empty); the wrong-category sword id is at index 1.
        Assert.Equal("(Empty)", live.HeadOptions[0].Name);
        Assert.StartsWith("Unknown Item", live.HeadOptions[1].Name);
        Assert.Equal(sword.Id, live.HeadOptions[1].Id);
    }

    // ===== Idempotency =====

    [Fact]
    public void Idempotent_Job_setter_does_not_mark_dirty()
    {
        var (vm, slot, _) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        live.SelectedJob = live.SelectedJob;

        Assert.False(vm.Model.IsDirty);
    }

    [Fact]
    public void Idempotent_Head_setter_does_not_mark_dirty()
    {
        var (vm, slot, _) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        live.SelectedHead = live.SelectedHead;

        Assert.False(vm.Model.IsDirty);
    }

    [Fact]
    public void Mutating_Live_Job_marks_parent_file_dirty()
    {
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        var newJob = live.JobOptions.First(j => j.Id != unit.Model.Job);
        live.SelectedJob = newJob;

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Mutating_Live_Secondary_marks_parent_file_dirty()
    {
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        var target = live.SecondaryOptions.First(c => c.Id != unit.Model.SecondaryAction);
        live.SelectedSecondary = target;

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Mutating_Live_Reaction_marks_parent_file_dirty()
    {
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        var target = live.ReactionOptions.First(a => a.Id != unit.Model.ReactionAbility);
        live.SelectedReaction = target;

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Mutating_Live_Head_marks_parent_file_dirty()
    {
        // Regression guard for the equipment dirty-marking bug discovered after
        // initial ship: UnitSaveData.NotifyOrQueue only fires entry-level INPC
        // (EquipItemEntry.Value/IsEmpty), not UnitSaveData.PropertyChanged for
        // the "EquipItems" collection name (that fires only on suspended bulk-op
        // exit). The fix is for LiveEditorViewModel to subscribe directly to each
        // EquipItemEntry's PropertyChanged in addition to UnitSaveData's.
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        var target = live.HeadOptions
            .First(i => i.Id != unit.Model.GetEquipItem(0) && i.Id != UnitSaveData.EmptyEquipSlotSentinel);
        live.SelectedHead = target;

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Mutating_Live_Rh_to_weapon_marks_parent_file_dirty()
    {
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        // Use a Sword that's NOT currently equipped in either RH slot — if we
        // happened to pick the same sword as the current RH weapon, the
        // idempotent setter would suppress INPC and (correctly) skip dirty.
        var currentWeapon = unit.Model.GetEquipItem(3);
        var currentShield = unit.Model.GetEquipItem(4);
        var weapon = live.RhOptions.First(i => i.ItemCategory == "Sword"
            && i.Id != currentWeapon
            && i.Id != currentShield);
        live.SelectedRh = weapon;

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Mutating_Live_Rh_to_shield_marks_parent_file_dirty()
    {
        var (vm, slot, _) = LoadBaselineWithPopulatedUnit();
        var live = GetLive(slot);
        Assert.False(vm.Model.IsDirty);

        var shield = live.RhOptions.First(i => i.ItemCategory == "Shield");
        live.SelectedRh = shield;

        Assert.True(vm.Model.IsDirty);
    }

    // ===== Real-fixture round-trip =====

    [Fact]
    public void Real_fixture_round_trip_persists_demuxed_Rh_write()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        slot.SelectedUnit = unit;
        var live = GetLive(slot);

        // Pick a Shield to force the demux to write into the SHIELD field
        // rather than the WEAPON field — meaningful regression guard for the
        // 5-slot UI ↔ 7-slot inline demux contract.
        var shield = live.RhOptions.First(i => i.ItemCategory == "Shield");
        live.SelectedRh = shield;

        var temp = Path.Combine(Path.GetTempPath(), $"tic-live-rh-roundtrip-{System.Guid.NewGuid():N}.png");
        try
        {
            vm.Model.SaveAs(temp);

            var reloadedBytes = File.ReadAllBytes(temp);
            var reloaded = SaveFileLoader.Load(reloadedBytes, temp);
            var reloadedVm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(reloaded, _fixture.Context);
            var reloadedSlot = reloadedVm.Slots[slot.Index];
            var reloadedUnit = reloadedSlot.Units.First(u => u.Index == unit.Index).Model;

            // RH shield slot (4) holds the new id; weapon slot (3) is cleared.
            Assert.Equal((ushort)shield.Id, reloadedUnit.GetEquipItem(4));
            Assert.Equal(UnitSaveData.EmptyEquipSlotSentinel, reloadedUnit.GetEquipItem(3));
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}

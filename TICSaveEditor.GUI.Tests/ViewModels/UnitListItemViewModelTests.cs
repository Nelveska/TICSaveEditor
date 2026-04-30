using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Exercises the name-resolution cascade in <see cref="UnitListItemViewModel.Name"/>.
/// Loads the Baseline fixture once, then mutates units' public properties (NameNo,
/// CharaNameKey, Character, Job, Sex) to coerce each branch.
/// </summary>
public class UnitListItemViewModelTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public UnitListItemViewModelTests(GameDataFixture fixture) => _fixture = fixture;

    private ManualSaveFileViewModel LoadBaseline()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        return Assert.IsType<ManualSaveFileViewModel>(
            SaveFileViewModelFactory.Create(save, _fixture.Context));
    }

    [Fact]
    public void Empty_unit_renders_as_empty_name()
    {
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var emptyUnit = slot.Units.First(u => u.IsEmpty);
        Assert.Equal(string.Empty, emptyUnit.Name);
    }

    [Fact]
    public void Hero_unit_short_circuits_to_Ramza()
    {
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        // Force the hero discriminator on a populated unit; Name should ignore NameNo.
        var unit = slot.Units.First(u => !u.IsEmpty);
        unit.Model.Character = 0x01;
        unit.Model.NameNo = 9999;
        Assert.Equal("Ramza", unit.Name);
    }

    [Fact]
    public void Generic_branch_fires_when_both_name_keys_are_zero()
    {
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        // Coerce: non-hero Character, zero both keys, deterministic Job + Sex.
        unit.Model.Character = 0x02;
        unit.Model.NameNo = 0;
        unit.Model.CharaNameKey = 0;
        unit.Model.Sex = 0x80; // high-bit set per glain finding 3 heuristic ⇒ Male
        Assert.StartsWith("Generic ", unit.Name);
        Assert.Contains("(Male)", unit.Name);
    }

    [Fact]
    public void Generic_branch_renders_Female_when_sex_high_bit_clear()
    {
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        unit.Model.Character = 0x02;
        unit.Model.NameNo = 0;
        unit.Model.CharaNameKey = 0;
        unit.Model.Sex = 0x00;
        Assert.Contains("(Female)", unit.Name);
    }

    [Fact]
    public void NameNo_lookup_returns_a_string_without_throwing()
    {
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        unit.Model.Character = 0x02;          // not hero
        unit.Model.NameNo = 7;                // some lookup attempt
        // Either resolves to a real name or returns "Unknown Character (NameNo 7)".
        // Both are acceptable per §8.7 invariants — the contract is "never throws".
        Assert.False(string.IsNullOrEmpty(unit.Name));
    }

    [Fact]
    public void JobName_resolves_via_GameDataContext()
    {
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        Assert.False(string.IsNullOrEmpty(unit.JobName));
    }

    [Fact]
    public void Mutating_Level_raises_INPC_for_LevelLabel_and_Level()
    {
        // Pre-M11-followup: the row was static. The bulk-op "Set all to level" mutated
        // bytes but the unit list didn't refresh until the user re-selected the slot.
        // After the followup, the row's INPC forwards Model.PropertyChanged.
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        var fired = new List<string?>();
        unit.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

        unit.Model.Level = (byte)(unit.Model.Level == 50 ? 51 : 50);

        Assert.Contains(nameof(UnitListItemViewModel.Level), fired);
        Assert.Contains(nameof(UnitListItemViewModel.LevelLabel), fired);
    }

    [Fact]
    public void Mutating_Job_raises_INPC_for_JobName_and_Name()
    {
        var vm = LoadBaseline();
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        var fired = new List<string?>();
        unit.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

        unit.Model.Job = (byte)(unit.Model.Job == 0 ? 1 : 0);

        Assert.Contains(nameof(UnitListItemViewModel.JobName), fired);
        Assert.Contains(nameof(UnitListItemViewModel.Name), fired);
    }

    // ===== Rename + active-filter coverage against the 10-slot enhanced.png =====
    // Per the user's notes 2026-04-30: renames are visible from save 2 onward;
    // affected unit indices are 1, 2, 3, 5 (units 4 and 6 not renamed but later
    // dismissed). Argath at slot 51 is active in save 3 and inactive (Resist=0xFF)
    // from save 4 onward.

    private ManualSaveFileViewModel LoadEnhancedAtRoot()
    {
        var path = SaveFixturePaths.EnhancedAtRoot();
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        return Assert.IsType<ManualSaveFileViewModel>(
            SaveFileViewModelFactory.Create(save, _fixture.Context));
    }

    [Fact]
    public void Renamed_unit_renders_UnitNickname_string()
    {
        var vm = LoadEnhancedAtRoot();
        var slot2 = vm.Slots[2];   // first save with renames present
        Assert.False(slot2.IsEmpty);

        // Per the T2 diagnostic dump: unit 1 = "Mateus", unit 2 = "Maeve",
        // unit 3 = "Vaucroix", unit 5 = "Yslande".
        Assert.Equal("Mateus", slot2.Units[1].Name);
        Assert.Equal("Maeve", slot2.Units[2].Name);
        Assert.Equal("Vaucroix", slot2.Units[3].Name);
        Assert.Equal("Yslande", slot2.Units[5].Name);
    }

    [Fact]
    public void Unrenamed_unit_falls_through_to_CharaNameKey_lookup()
    {
        var vm = LoadEnhancedAtRoot();
        var slot2 = vm.Slots[2];
        // Unit 4 is not renamed in the fixture (verified via T2 dump).
        // Falls through to NameNo (=0, skipped) → CharaNameKey lookup.
        var name = slot2.Units[4].Name;
        Assert.False(string.IsNullOrEmpty(name));
        Assert.NotEqual("Mateus", name);  // unique-rename sanity check
    }

    [Fact]
    public void Hero_unit_short_circuits_even_with_populated_ChrNameRaw()
    {
        var vm = LoadEnhancedAtRoot();
        var slot2 = vm.Slots[2];
        // Ramza's ChrNameRaw is empirically zero across all 10 saves, so the
        // short-circuit isn't actually fighting against UnitNickname here —
        // but the test pins the cascade order.
        Assert.Equal(0x01, slot2.Units[0].Model.Character);
        Assert.Equal("Ramza", slot2.Units[0].Name);
    }

    [Fact]
    public void IsActive_tracks_argath_departure_at_slot_51()
    {
        var vm = LoadEnhancedAtRoot();

        // Save 3: Argath joins (Resist=0x33=51). Save 4 onward: Resist=0xFF.
        Assert.True(vm.Slots[3].Units[51].IsActive,
            "save 3 should have Argath active in slot 51");
        Assert.False(vm.Slots[4].Units[51].IsActive,
            "save 4 should have Argath inactive (Resist=0xFF)");
        Assert.False(vm.Slots[9].Units[51].IsActive,
            "save 9 should have Argath inactive");

        // Slot 51 in save 4 IS NOT empty (Character byte still 0x07), but IS inactive.
        Assert.False(vm.Slots[4].Units[51].IsEmpty);
        Assert.True(vm.Slots[4].Units[51].IsInactive);
    }

    [Fact]
    public void IsActive_tracks_dismissed_recruits_at_slots_4_and_6()
    {
        var vm = LoadEnhancedAtRoot();

        // Saves 0 and 1: female recruits at slots 4 and 6 active. Save 2 onward:
        // dismissed (Resist=0xFF) but Character byte preserved.
        Assert.True(vm.Slots[0].Units[4].IsActive);
        Assert.True(vm.Slots[1].Units[6].IsActive);
        Assert.False(vm.Slots[2].Units[4].IsActive);
        Assert.False(vm.Slots[2].Units[6].IsActive);
        Assert.False(vm.Slots[2].Units[4].IsEmpty);  // still has Character data
        Assert.False(vm.Slots[2].Units[6].IsEmpty);
    }

    [Fact]
    public void Mutating_Resist_raises_INPC_for_IsActive_and_IsInactive()
    {
        var vm = LoadEnhancedAtRoot();
        var unit = vm.Slots[3].Units[51];   // active Argath
        var fired = new List<string?>();
        unit.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

        unit.Model.Resist = 0xFF;            // simulate the departure transition

        Assert.Contains(nameof(UnitListItemViewModel.IsActive), fired);
        Assert.Contains(nameof(UnitListItemViewModel.IsInactive), fired);
    }

    [Fact]
    public void Mutating_ChrNameRaw_raises_INPC_for_Name()
    {
        var vm = LoadEnhancedAtRoot();
        var unit = vm.Slots[2].Units[1];  // "Mateus"
        var fired = new List<string?>();
        unit.PropertyChanged += (_, e) => fired.Add(e.PropertyName);

        var newBytes = new byte[64];
        var ascii = System.Text.Encoding.ASCII.GetBytes("Cidolfus");
        Array.Copy(ascii, newBytes, ascii.Length);
        unit.Model.ChrNameRaw = newBytes;

        Assert.Contains(nameof(UnitListItemViewModel.Name), fired);
        Assert.Equal("Cidolfus", unit.Name);
    }
}

using System.IO;
using System.Linq;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Exercises the CombatSet editor VM against the Baseline real-save fixture.
/// Construction is via the SaveSlotViewModel.SelectedUnitEditor lazy cache —
/// matches the production wiring path (UnitListView ListBox SelectedItem →
/// SaveSlotViewModel.SelectedUnit → SelectedUnitEditor). Tests cover:
/// - 3-preset surface
/// - selection lifecycle (null/empty collapse, cached reference equality)
/// - two-way binding round-trip (Name, Job, Skillset, Ability fields)
/// - dropdown filtering (AbilityType for R/S/M, null-name for Job/Skillset)
/// - synthetic Unknown ID prepended when current value isn't in the *filtered* list
///   (covers mod IDs and wrong-type abilities placed in the wrong slot)
/// - dirty-marking via parent SaveFile (idempotent setter contract preserved)
/// - real-fixture mutate → save → reload round-trip
/// </summary>
public class CombatSetEditorViewModelTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public CombatSetEditorViewModelTests(GameDataFixture fixture) => _fixture = fixture;

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

    [Fact]
    public void Editor_exposes_three_presets()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor;
        Assert.NotNull(editor);
        Assert.Equal(3, editor!.Presets.Count);
    }

    [Fact]
    public void Empty_unit_has_null_editor()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var emptyUnit = slot.Units.First(u => u.IsEmpty);
        slot.SelectedUnit = emptyUnit;
        Assert.Null(slot.SelectedUnitEditor);
    }

    [Fact]
    public void Null_selection_has_null_editor()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        Assert.NotNull(slot.SelectedUnitEditor);
        slot.SelectedUnit = null;
        Assert.Null(slot.SelectedUnitEditor);
    }

    [Fact]
    public void Selection_cycle_returns_cached_editor_reference()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var first = slot.SelectedUnitEditor;
        slot.SelectedUnit = null;
        slot.SelectedUnit = unit;
        var second = slot.SelectedUnitEditor;
        Assert.Same(first, second);
    }

    [Fact]
    public void Distinct_units_yield_distinct_cached_editors()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var firstEditor = slot.SelectedUnitEditor;
        var otherUnit = slot.Units.First(u => !u.IsEmpty && u.Model != unit.Model);
        slot.SelectedUnit = otherUnit;
        var secondEditor = slot.SelectedUnitEditor;
        Assert.NotSame(firstEditor, secondEditor);
    }

    [Fact]
    public void Setting_Name_mutates_underlying_CombatSet()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        editor.Presets[0].Name = "TankBuild";
        Assert.Equal("TankBuild", unit.Model.CombatSets[0].Name);
    }

    [Fact]
    public void Setting_SelectedJob_mutates_underlying_Job_byte()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var targetJob = editor.Presets[0].JobOptions.First(j => j.Id != unit.Model.CombatSets[0].Job);
        editor.Presets[0].SelectedJob = targetJob;
        Assert.Equal((byte)targetJob.Id, unit.Model.CombatSets[0].Job);
    }

    [Fact]
    public void Setting_SelectedSkillset0_mutates_underlying_short()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var target = editor.Presets[0].SkillsetOptions.First(c => c.Id != unit.Model.CombatSets[0].Skillset0);
        editor.Presets[0].SelectedSkillset0 = target;
        Assert.Equal((short)target.Id, unit.Model.CombatSets[0].Skillset0);
    }

    [Fact]
    public void Setting_SelectedReaction_mutates_underlying_ushort()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var target = editor.Presets[0].ReactionOptions.First(a => a.Id != unit.Model.CombatSets[0].ReactionAbility);
        editor.Presets[0].SelectedReaction = target;
        Assert.Equal((ushort)target.Id, unit.Model.CombatSets[0].ReactionAbility);
    }

    [Fact]
    public void JobOptions_prepends_synthetic_Unknown_when_current_id_missing()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);

        // Coerce a Job byte that's outside the bundled table BEFORE selecting the
        // unit. The editor builds option lists at construction; subsequent
        // selection triggers lazy-cache via SelectedUnitEditor getter.
        unit.Model.CombatSets[0].Job = 0xFE;
        Assert.False(_fixture.Context.TryGetJob(0xFE, out _));

        slot.SelectedUnit = unit;
        var editor = slot.SelectedUnitEditor!;
        var first = editor.Presets[0].JobOptions[0];
        Assert.Equal(0xFE, first.Id);
        Assert.StartsWith("Unknown Job", first.Name);
    }

    // ===== Dropdown filtering coverage (added 2026-05-01 usability fix pass) =====

    [Fact]
    public void ReactionOptions_only_contains_Reaction_type_abilities()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var entry = editor.Presets[0];
        // Allow the synthetic Unknown sentinel (empty AbilityType); every other
        // entry must be exact-match "Reaction".
        foreach (var ability in entry.ReactionOptions)
        {
            if (ability.Name.StartsWith("Unknown Ability")) continue;
            Assert.Equal("Reaction", ability.AbilityType);
        }
    }

    [Fact]
    public void SupportOptions_only_contains_Support_type_abilities()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var entry = editor.Presets[0];
        foreach (var ability in entry.SupportOptions)
        {
            if (ability.Name.StartsWith("Unknown Ability")) continue;
            Assert.Equal("Support", ability.AbilityType);
        }
    }

    [Fact]
    public void MovementOptions_only_contains_Movement_type_abilities()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var entry = editor.Presets[0];
        foreach (var ability in entry.MovementOptions)
        {
            if (ability.Name.StartsWith("Unknown Ability")) continue;
            Assert.Equal("Movement", ability.AbilityType);
        }
    }

    [Fact]
    public void SkillsetOptions_excludes_null_or_empty_named_entries()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var entry = editor.Presets[0];
        Assert.All(entry.SkillsetOptions, c => Assert.False(string.IsNullOrEmpty(c.Name)));
    }

    [Fact]
    public void JobOptions_excludes_null_or_empty_named_entries()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var entry = editor.Presets[0];
        Assert.All(entry.JobOptions, j => Assert.False(string.IsNullOrEmpty(j.Name)));
    }

    [Fact]
    public void Synthetic_Unknown_inserted_when_current_reaction_id_is_wrong_type()
    {
        // Place a Support-typed ability ID into the Reaction slot. Filtered
        // ReactionOptions won't include that ID, so the synthetic Unknown must
        // prepend to keep SelectedItem non-null and round-trip preserved.
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);

        var supportAbility = _fixture.Context.Abilities.First(a => a.AbilityType == "Support" && !string.IsNullOrEmpty(a.Name));
        unit.Model.CombatSets[0].ReactionAbility = (ushort)supportAbility.Id;

        slot.SelectedUnit = unit;
        var editor = slot.SelectedUnitEditor!;
        var first = editor.Presets[0].ReactionOptions[0];
        Assert.Equal(supportAbility.Id, first.Id);
        Assert.StartsWith("Unknown Ability", first.Name);
    }

    // ===== Second-round dropdown filtering refinements (added 2026-05-01) =====

    [Fact]
    public void SkillsetOptions_excludes_basic_battle_commands()
    {
        // JobCommand Keys 1-3 (Attack / Evasive Stance / Reequip) are universal
        // battle-menu commands, not skillsets. Real skillsets start at Key 5.
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var entry = slot.SelectedUnitEditor!.Presets[0];
        foreach (var c in entry.SkillsetOptions)
        {
            if (c.Name.StartsWith("Unknown Skillset")) continue;  // synthetic sentinel
            Assert.True(c.Id > 3, $"Skillset Id {c.Id} ({c.Name}) is a basic battle command, not a skillset.");
        }
    }

    [Fact]
    public void Ability_dropdowns_exclude_MARKED_FOR_DELETION_entries()
    {
        // The bundled Ability.json has one entry whose Name starts with
        // "MARKED FOR DELETION - REPORT IF DISPLAYED" (Key 440, AbilityType=Reaction).
        // Localization-team marker for cut content; must not appear in any combo.
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var entry = slot.SelectedUnitEditor!.Presets[0];
        foreach (var combo in new[] { entry.ReactionOptions, entry.SupportOptions, entry.MovementOptions })
            Assert.DoesNotContain(combo, a => a.Name.StartsWith("MARKED FOR DELETION", System.StringComparison.Ordinal));
    }

    [Fact]
    public void Ability_dropdowns_exclude_crash_prone_and_nonfunctional_ids()
    {
        // Per user 2026-05-01: Keys 0, 509, 510 (offsets 0x0000, 0x01FD, 0x01FE)
        // crash the game when used. Key 508 ("Stealth") is functional but applied
        // through a different code path; setting it via this editor's CombatSet
        // slot is non-functional. All four must be filtered from R/S/M combos.
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var entry = slot.SelectedUnitEditor!.Presets[0];
        var blocked = new[] { 0, 508, 509, 510 };
        foreach (var combo in new[] { entry.ReactionOptions, entry.SupportOptions, entry.MovementOptions })
        foreach (var id in blocked)
            Assert.DoesNotContain(combo, a => a.Id == id && !a.Name.StartsWith("Unknown Ability"));
    }

    [Fact]
    public void Support_combo_includes_renamed_CT0_NoCharge()
    {
        // Key 483 has placeholder Name "A483" but is the functional debug utility
        // "CT 0" / "No Charge" (per user 2026-05-01). The editor must rename the
        // display Name via record-with cloning; entry should appear in Support combo.
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var entry = slot.SelectedUnitEditor!.Presets[0];
        var renamed = entry.SupportOptions.FirstOrDefault(a => a.Id == 483);
        Assert.NotNull(renamed);
        Assert.Equal("CT0/No Charge", renamed!.Name);
    }

    [Fact]
    public void Ability_dropdowns_exclude_unrenamed_placeholder_named_entries()
    {
        // Regression guard: any A### placeholder-named ability that doesn't have
        // an entry in the AbilityRenames map must be filtered out. Currently the
        // only A### Name entries are Key 483 (renamed → "CT0/No Charge") and
        // Key 508 (excluded as non-functional Stealth). If a future bundle update
        // adds new A### Names without rename overrides, this test catches them.
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var entry = slot.SelectedUnitEditor!.Presets[0];
        var rx = new System.Text.RegularExpressions.Regex(@"^A\d+$");
        foreach (var combo in new[] { entry.ReactionOptions, entry.SupportOptions, entry.MovementOptions })
        {
            var leak = combo.FirstOrDefault(a => rx.IsMatch(a.Name));
            Assert.Null(leak);
        }
    }

    [Fact]
    public void Mutating_preset_marks_parent_file_dirty()
    {
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        Assert.False(vm.Model.IsDirty);

        editor.Presets[0].Name = "DirtyTest";

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Idempotent_setter_does_not_mark_dirty()
    {
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var editor = slot.SelectedUnitEditor!;
        var entry = editor.Presets[0];
        var originalJob = entry.SelectedJob;
        Assert.NotNull(originalJob);
        Assert.False(vm.Model.IsDirty);

        // Same-value write through SelectedJob — UnitSaveData.SetCombatSetJob has
        // an explicit idempotency check ("if (slot.Job == value) return;") that
        // suppresses both the model PropertyChanged AND the downstream MarkDirty.
        // Other CS typed setters (Skillset/Ability) follow the same pattern.
        // Name is the lone exception today (re-writes the 16-byte buffer
        // unconditionally and fires NotifyOrQueue) — flagged for follow-up; not
        // a v0.1 blocker since user-driven name edits are uncommon and dirty-on-
        // no-op only worsens save-prompt UX, not correctness.
        entry.SelectedJob = originalJob;

        Assert.False(vm.Model.IsDirty);
    }

    [Fact]
    public void Real_fixture_round_trip_persists_edited_preset_name()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var slot = vm.Slots.First(s => !s.IsEmpty);
        var unit = slot.Units.First(u => !u.IsEmpty);
        slot.SelectedUnit = unit;
        var editor = slot.SelectedUnitEditor!;

        const string edited = "RoundTrip!";
        editor.Presets[0].Name = edited;

        var temp = Path.Combine(Path.GetTempPath(), $"tic-cs-editor-{System.Guid.NewGuid():N}.png");
        try
        {
            vm.Model.SaveAs(temp);

            var reloadedBytes = File.ReadAllBytes(temp);
            var reloaded = SaveFileLoader.Load(reloadedBytes, temp);
            var reloadedVm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(reloaded, _fixture.Context);
            var reloadedSlot = reloadedVm.Slots[slot.Index];
            var reloadedUnit = reloadedSlot.Units.First(u => u.Index == unit.Index).Model;

            Assert.Equal(edited, reloadedUnit.CombatSets[0].Name);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}

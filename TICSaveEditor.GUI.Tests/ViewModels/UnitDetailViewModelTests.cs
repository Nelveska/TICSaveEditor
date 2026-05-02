using System.IO;
using System.Linq;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Exercises the composite UnitDetailViewModel — the per-unit TabControl host
/// that wraps Live + CombatSets editors. Construction is via SaveSlotViewModel's
/// lazy detail cache; the cache itself is what's reference-stable across
/// re-selections (children are stable by composition).
/// </summary>
public class UnitDetailViewModelTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public UnitDetailViewModelTests(GameDataFixture fixture) => _fixture = fixture;

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
    public void Construction_exposes_Live_and_CombatSets_non_null()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var detail = slot.SelectedUnitDetail;
        Assert.NotNull(detail);
        Assert.NotNull(detail!.Live);
        Assert.NotNull(detail.CombatSets);
    }

    [Fact]
    public void UnitDisplayName_is_non_empty_for_populated_unit()
    {
        var (_, slot, _) = LoadBaselineWithPopulatedUnit();
        var detail = slot.SelectedUnitDetail!;
        Assert.False(string.IsNullOrEmpty(detail.UnitDisplayName));
    }

    [Fact]
    public void Mutating_Live_marks_parent_file_dirty()
    {
        var (vm, slot, unit) = LoadBaselineWithPopulatedUnit();
        var detail = slot.SelectedUnitDetail!;
        Assert.False(vm.Model.IsDirty);

        var newJob = detail.Live.JobOptions.First(j => j.Id != unit.Model.Job);
        detail.Live.SelectedJob = newJob;

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Mutating_CombatSets_preset_marks_parent_file_dirty()
    {
        var (vm, slot, _) = LoadBaselineWithPopulatedUnit();
        var detail = slot.SelectedUnitDetail!;
        Assert.False(vm.Model.IsDirty);

        detail.CombatSets.Presets[0].Name = "DirtyTestUD";

        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void Cached_detail_reference_stable_across_reselection()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var first = slot.SelectedUnitDetail;
        slot.SelectedUnit = null;
        slot.SelectedUnit = unit;
        var second = slot.SelectedUnitDetail;
        Assert.Same(first, second);
        Assert.Same(first!.Live, second!.Live);
        Assert.Same(first.CombatSets, second.CombatSets);
    }

    [Fact]
    public void Mutating_Live_does_not_affect_CombatSets_preset_0()
    {
        var (_, slot, unit) = LoadBaselineWithPopulatedUnit();
        var detail = slot.SelectedUnitDetail!;
        var preset0NameBefore = detail.CombatSets.Presets[0].Name;
        var preset0JobBefore = detail.CombatSets.Presets[0].Model.Job;

        var newJob = detail.Live.JobOptions.First(j => j.Id != unit.Model.Job);
        detail.Live.SelectedJob = newJob;

        // Inline edits must not bleed into the preset state.
        Assert.Equal(preset0NameBefore, detail.CombatSets.Presets[0].Name);
        Assert.Equal(preset0JobBefore, detail.CombatSets.Presets[0].Model.Job);
    }
}

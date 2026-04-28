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
}

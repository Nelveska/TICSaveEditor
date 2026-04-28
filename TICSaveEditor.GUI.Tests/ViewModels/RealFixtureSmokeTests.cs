using System.IO;
using System.Linq;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Milestone-validating smoke test: each of the 5 fixture saves loads, the
/// resulting <see cref="ManualSaveFileViewModel"/> has 50 slots, every populated
/// slot has 54 units, and every unit's Name/JobName/Level resolves without throw.
///
/// Uses <see cref="SaveFileLoader.Load(byte[], string)"/> (byte-array overload)
/// to skip the on-disk backup that the string overload triggers — same reason
/// CLI uses the byte-array path (per <c>decisions_m10_gui_tests_project.md</c>).
/// </summary>
public class RealFixtureSmokeTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public RealFixtureSmokeTests(GameDataFixture fixture) => _fixture = fixture;

    public static IEnumerable<object[]> Fixtures =>
        SaveFixturePaths.FixtureNames.Select(n => new object[] { n });

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Loads_and_populates_slot_list_without_throwing(string fixtureName)
    {
        var path = SaveFixturePaths.Enhanced(fixtureName);
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);

        var vm = SaveFileViewModelFactory.Create(save, _fixture.Context);
        var manual = Assert.IsType<ManualSaveFileViewModel>(vm);
        Assert.Equal(ManualSaveFile.SlotCount, manual.Slots.Count);

        // At least one slot is populated in every fixture.
        var populated = manual.Slots.Where(s => !s.IsEmpty).ToList();
        Assert.NotEmpty(populated);

        foreach (var slot in populated)
        {
            Assert.Equal(BattleSection.UnitCount, slot.Units.Count);

            // Slot proxies don't throw.
            _ = slot.Title;
            _ = slot.HeroName;
            _ = slot.SaveTimestampDisplay;
            _ = slot.PlaytimeDisplay;

            // Every unit's resolution path is exercised — empty rows return string.Empty,
            // populated rows hit hero / NameNo / CharaNameKey / Generic branches.
            foreach (var unit in slot.Units)
            {
                _ = unit.Name;
                _ = unit.JobName;
                _ = unit.Level;
                _ = unit.IsEmpty;
            }
        }
    }
}

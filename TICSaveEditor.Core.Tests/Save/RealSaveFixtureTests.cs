using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Tests against the 4 user-supplied real game saves under <c>SaveFiles/</c>.
/// See <c>memory/decisions_umif_realfixture_locations.md</c> for the capture protocol
/// and what each variant represents (Baseline, ChangeOneItem, ChangeOneAbilitySlot,
/// ChangeOneSkillset). The 2026-05-01 fixture set replaces an earlier 5-fixture
/// battery whose findings are now baked into Core code + memory; the original
/// EquipSet/InternalChecksum/Inventory/JobChange fixtures were retired in the
/// same session that resolved the CombatSet decomposition.
/// </summary>
public class RealSaveFixtureTests
{
    private static string FixturePath(string name) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "SaveFiles", name, "enhanced.png");

    [Theory]
    [InlineData("Baseline")]
    [InlineData("ChangeOneItem")]
    [InlineData("ChangeOneAbilitySlot")]
    [InlineData("ChangeOneSkillset")]
    public void Loads_real_save_as_ManualSaveFile_with_50_slots(string fixture)
    {
        var bytes = File.ReadAllBytes(FixturePath(fixture));
        var save = SaveFileLoader.Load(bytes, FixturePath(fixture));

        var manual = Assert.IsType<ManualSaveFile>(save);
        Assert.Equal(50, manual.Slots.Count);
    }

    [Theory]
    [InlineData("Baseline")]
    [InlineData("ChangeOneItem")]
    [InlineData("ChangeOneAbilitySlot")]
    [InlineData("ChangeOneSkillset")]
    public void No_mutation_round_trip_byte_identical(string fixture)
    {
        var sourcePath = FixturePath(fixture);
        var sourceBytes = File.ReadAllBytes(sourcePath);

        var save = SaveFileLoader.Load(sourceBytes, sourcePath);
        var tempPath = Path.GetTempFileName() + ".png";
        try
        {
            save.SaveAs(tempPath);
            var roundTrippedBytes = File.ReadAllBytes(tempPath);
            Assert.Equal(sourceBytes, roundTrippedBytes);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void Baseline_has_at_least_one_populated_slot()
    {
        var bytes = File.ReadAllBytes(FixturePath("Baseline"));
        var save = (ManualSaveFile)SaveFileLoader.Load(bytes, FixturePath("Baseline"));

        var populated = save.Slots.Count(s => !s.IsEmpty);
        Assert.True(populated >= 1, $"Baseline should have at least 1 populated slot; found {populated}.");
    }

    [Fact]
    public void Variant_files_have_more_populated_slots_than_baseline()
    {
        // Each variant was captured by saving to a NEW slot after the isolated change,
        // so each variant has exactly one more populated slot than baseline.
        var baseline = (ManualSaveFile)SaveFileLoader.Load(
            File.ReadAllBytes(FixturePath("Baseline")), FixturePath("Baseline"));
        var baselinePopulated = baseline.Slots.Count(s => !s.IsEmpty);

        foreach (var fixture in new[] { "ChangeOneItem", "ChangeOneAbilitySlot", "ChangeOneSkillset" })
        {
            var save = (ManualSaveFile)SaveFileLoader.Load(
                File.ReadAllBytes(FixturePath(fixture)), FixturePath(fixture));
            var populated = save.Slots.Count(s => !s.IsEmpty);
            Assert.True(populated > baselinePopulated,
                $"{fixture} should have more populated slots than baseline ({populated} <= {baselinePopulated}).");
        }
    }
}

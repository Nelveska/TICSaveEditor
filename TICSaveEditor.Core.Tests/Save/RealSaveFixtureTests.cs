using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Tests against the 5 user-supplied real game saves under <c>SaveFiles/</c>.
/// See <c>memory/decisions_umif_realfixture_locations.md</c> for the capture protocol
/// and what each variant represents (Baseline, EquipSet, InternalChecksum, Inventory, JobChange).
/// </summary>
public class RealSaveFixtureTests
{
    private static string FixturePath(string name) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "SaveFiles", name, "enhanced.png");

    [Theory]
    [InlineData("Baseline")]
    [InlineData("EquipSet")]
    [InlineData("InternalChecksum")]
    [InlineData("Inventory")]
    [InlineData("JobChange")]
    public void Loads_real_save_as_ManualSaveFile_with_50_slots(string fixture)
    {
        var bytes = File.ReadAllBytes(FixturePath(fixture));
        var save = SaveFileLoader.Load(bytes, FixturePath(fixture));

        var manual = Assert.IsType<ManualSaveFile>(save);
        Assert.Equal(50, manual.Slots.Count);
    }

    [Theory]
    [InlineData("Baseline")]
    [InlineData("EquipSet")]
    [InlineData("InternalChecksum")]
    [InlineData("Inventory")]
    [InlineData("JobChange")]
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
        // The user described capturing each variant by saving to a NEW slot, so each
        // successive file should have a higher non-empty slot count than the baseline.
        var baseline = (ManualSaveFile)SaveFileLoader.Load(
            File.ReadAllBytes(FixturePath("Baseline")), FixturePath("Baseline"));
        var baselinePopulated = baseline.Slots.Count(s => !s.IsEmpty);

        foreach (var fixture in new[] { "EquipSet", "InternalChecksum", "Inventory", "JobChange" })
        {
            var save = (ManualSaveFile)SaveFileLoader.Load(
                File.ReadAllBytes(FixturePath(fixture)), FixturePath(fixture));
            var populated = save.Slots.Count(s => !s.IsEmpty);
            Assert.True(populated > baselinePopulated,
                $"{fixture} should have more populated slots than baseline ({populated} <= {baselinePopulated}).");
        }
    }
}

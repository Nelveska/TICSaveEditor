using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Regression guard: <see cref="UnitSaveData.Validate"/> rules are only as good
/// as the byte values they're tested against. M5's ValidationTests synthesize
/// units from <c>new byte[600]</c> and never exercise real-game byte patterns
/// — that's how the M4 Zodiac rule (which compared the raw byte against the
/// 0..11 sign range, missing the high-nibble decode) shipped uncaught.
///
/// This test loads each of the 5 real save fixtures, iterates every populated
/// unit, and asserts no validation issue fires for fields whose semantics we
/// understand from real-fixture inspection. Fields that may legitimately
/// produce issues on real saves (e.g., guests with placeholder bytes) are
/// excluded narrowly — the test guards specific known-decode fields.
/// </summary>
public class RealFixtureValidationTests
{
    [Theory]
    [InlineData("Baseline")]
    [InlineData("EquipSet")]
    [InlineData("InternalChecksum")]
    [InlineData("Inventory")]
    [InlineData("JobChange")]
    public void Real_fixture_units_have_no_Zodiac_validation_errors(string fixture)
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", fixture, "enhanced.png");
        var save = (ManualSaveFile)SaveFileLoader.Load(File.ReadAllBytes(path), path);

        foreach (var slot in save.Slots)
        {
            if (slot.IsEmpty) continue;
            for (int unitIndex = 0; unitIndex < slot.SaveWork.Battle.Units.Count; unitIndex++)
            {
                var unit = slot.SaveWork.Battle.Units[unitIndex];
                if (unit.IsEmpty) continue;
                var issues = unit.Validate().Issues;
                Assert.DoesNotContain(issues,
                    i => i.FieldName == nameof(UnitSaveData.ZodiacSign));
            }
        }
    }
}
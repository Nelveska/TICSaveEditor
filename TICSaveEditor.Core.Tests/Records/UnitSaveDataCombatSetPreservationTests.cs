using System.Collections.ObjectModel;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataCombatSetPreservationTests
{
    [Fact]
    public void CombatSet_region_0x126_through_0x22D_round_trips_byte_identical()
    {
        var rng = new Random(2025);
        var bytes = new byte[600];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var unit = new UnitSaveData(bytes);
        // Mutate something outside the CombatSet region to ensure the unit is "live".
        unit.Level = 50;

        var output = new byte[600];
        unit.WriteTo(output);

        for (int i = 0x126; i <= 0x22D; i++)
            Assert.Equal(pristine[i], output[i]);
    }

    [Fact]
    public void UnitSaveData_exposes_three_CombatSets_in_milestone_6()
    {
        var prop = typeof(UnitSaveData).GetProperty(nameof(UnitSaveData.CombatSets));
        Assert.NotNull(prop);
        Assert.Equal(typeof(ReadOnlyObservableCollection<CombatSet>), prop!.PropertyType);

        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(3, unit.CombatSets.Count);
    }
}

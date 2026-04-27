using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records.Entries;

public class JobPointEntryTests
{
    [Fact]
    public void Setting_value_routes_through_owner_SetJobPoint()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.JobPoints[5];
        entry.Value = 1234;
        Assert.Equal(1234, unit.GetJobPoint(5));
    }

    [Fact]
    public void Owner_calling_SetJobPoint_fires_Value_PropertyChanged_on_entry()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.JobPoints[7];
        var hits = 0;
        entry.PropertyChanged += (_, e) => { if (e.PropertyName == "Value") hits++; };

        unit.SetJobPoint(7, 42);

        Assert.Equal(1, hits);
    }
}

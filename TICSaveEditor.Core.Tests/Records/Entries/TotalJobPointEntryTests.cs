using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records.Entries;

public class TotalJobPointEntryTests
{
    [Fact]
    public void Setting_value_routes_through_owner_SetTotalJobPoint()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.TotalJobPoints[10];
        entry.Value = 5678;
        Assert.Equal(5678, unit.GetTotalJobPoint(10));
    }
}

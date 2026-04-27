using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records.Entries;

public class JobLevelEntryTests
{
    [Fact]
    public void Setting_value_routes_through_owner_SetJobLevel()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.JobLevels[3];
        entry.Value = 8;
        Assert.Equal(8, unit.GetJobLevel(3));
    }
}

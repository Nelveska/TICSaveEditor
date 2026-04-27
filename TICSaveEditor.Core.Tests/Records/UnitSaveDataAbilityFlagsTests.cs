using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataAbilityFlagsTests
{
    [Fact]
    public void AbilityFlags_collection_has_exactly_22_entries()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(22, unit.AbilityFlags.Count);
    }

    [Fact]
    public void AbilityFlags_indexer_returns_entry_with_matching_JobId()
    {
        var unit = new UnitSaveData(new byte[600]);
        for (int i = 0; i < 22; i++) Assert.Equal(i, unit.AbilityFlags[i].JobId);
    }
}

using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataIsEmptyTests
{
    [Fact]
    public void IsEmpty_true_when_Character_is_zero()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.True(unit.IsEmpty);
    }

    [Fact]
    public void IsEmpty_false_when_Character_is_one()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 1;
        Assert.False(unit.IsEmpty);
    }
}

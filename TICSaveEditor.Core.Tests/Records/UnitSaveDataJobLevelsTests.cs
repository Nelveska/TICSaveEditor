using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataJobLevelsTests
{
    [Fact]
    public void GetJobLevel_returns_zero_for_blank()
    {
        var unit = new UnitSaveData(new byte[600]);
        for (int i = 0; i < 12; i++) Assert.Equal(0, unit.GetJobLevel(i));
    }

    [Fact]
    public void SetJobLevel_round_trips_at_offset_0x74_plus_index()
    {
        var unit = new UnitSaveData(new byte[600]);
        for (int i = 0; i < 12; i++) unit.SetJobLevel(i, (byte)(i + 1));

        var output = new byte[600];
        unit.WriteTo(output);

        for (int i = 0; i < 12; i++) Assert.Equal(i + 1, output[0x74 + i]);
    }

    [Fact]
    public void SetJobLevel_throws_on_index_outside_0_through_11()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.SetJobLevel(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.SetJobLevel(12, 1));
    }
}

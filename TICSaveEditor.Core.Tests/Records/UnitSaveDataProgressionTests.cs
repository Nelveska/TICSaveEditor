using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataProgressionTests
{
    [Fact]
    public void Exp_Level_StartBcp_StartFaith_round_trip_at_offsets_0x1C_through_0x1F()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Exp = 0xAA;
        unit.Level = 50;
        unit.StartBcp = 60;
        unit.StartFaith = 70;

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0xAA, output[0x1C]);
        Assert.Equal(50, output[0x1D]);
        Assert.Equal(60, output[0x1E]);
        Assert.Equal(70, output[0x1F]);
    }

    [Fact]
    public void Setting_Level_raises_property_changed_once()
    {
        var unit = new UnitSaveData(new byte[600]);
        var hits = 0;
        unit.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(UnitSaveData.Level)) hits++; };

        unit.Level = 99;

        Assert.Equal(1, hits);
    }

    [Fact]
    public void Setting_Level_does_not_disturb_stats_block()
    {
        var rng = new Random(31);
        var bytes = new byte[600];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var unit = new UnitSaveData(bytes);
        unit.Level = 42;

        var output = new byte[600];
        unit.WriteTo(output);

        for (int i = 0x20; i < 0x32; i++)
            Assert.Equal(pristine[i], output[i]);
    }
}

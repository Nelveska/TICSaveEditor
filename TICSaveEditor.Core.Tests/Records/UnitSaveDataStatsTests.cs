using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataStatsTests
{
    [Fact]
    public void HpMaxBase_zero_reads_as_zero()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(0, unit.HpMaxBase);
    }

    [Fact]
    public void HpMaxBase_max_24bit_value_reads_as_0xFFFFFF()
    {
        var bytes = new byte[600];
        bytes[0x20] = 0xFF;
        bytes[0x21] = 0xFF;
        bytes[0x22] = 0xFF;
        var unit = new UnitSaveData(bytes);
        Assert.Equal(0xFFFFFF, unit.HpMaxBase);
    }

    [Fact]
    public void HpMaxBase_writes_low_middle_high_bytes_in_LE_order()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.HpMaxBase = 0x086630; // 550,064 — observed Ramza L2 value

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0x30, output[0x20]);
        Assert.Equal(0x66, output[0x21]);
        Assert.Equal(0x08, output[0x22]);
    }

    [Fact]
    public void HpMaxBase_setter_throws_for_negative_value()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.HpMaxBase = -1);
    }

    [Fact]
    public void HpMaxBase_setter_throws_for_value_greater_than_0xFFFFFF()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.HpMaxBase = 0x1000000);
    }

    [Fact]
    public void Each_3byte_field_is_independent_at_offsets_0x20_0x23_0x26_0x29_0x2C_0x2F()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.HpMaxBase = 0x010203;
        unit.MpMaxBase = 0x040506;
        unit.WtBase = 0x070809;
        unit.AtBase = 0x0A0B0C;
        unit.MatBase = 0x0D0E0F;
        unit.JobChangeFlag = 0x101112;

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0x03, output[0x20]); Assert.Equal(0x02, output[0x21]); Assert.Equal(0x01, output[0x22]);
        Assert.Equal(0x06, output[0x23]); Assert.Equal(0x05, output[0x24]); Assert.Equal(0x04, output[0x25]);
        Assert.Equal(0x09, output[0x26]); Assert.Equal(0x08, output[0x27]); Assert.Equal(0x07, output[0x28]);
        Assert.Equal(0x0C, output[0x29]); Assert.Equal(0x0B, output[0x2A]); Assert.Equal(0x0A, output[0x2B]);
        Assert.Equal(0x0F, output[0x2C]); Assert.Equal(0x0E, output[0x2D]); Assert.Equal(0x0D, output[0x2E]);
        Assert.Equal(0x12, output[0x2F]); Assert.Equal(0x11, output[0x30]); Assert.Equal(0x10, output[0x31]);
    }

    [Fact]
    public void JobChangeFlag_round_trips_as_24bit_value()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.JobChangeFlag = 0xABCDEF;
        Assert.Equal(0xABCDEF, unit.JobChangeFlag);
    }

    [Fact]
    public void Setting_HpMaxBase_does_not_disturb_MpMaxBase()
    {
        var bytes = new byte[600];
        bytes[0x23] = 0x99;
        bytes[0x24] = 0x88;
        bytes[0x25] = 0x77;

        var unit = new UnitSaveData(bytes);
        unit.HpMaxBase = 0x123456;

        Assert.Equal(0x778899, unit.MpMaxBase);
    }
}

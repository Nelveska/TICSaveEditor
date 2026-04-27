using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class WorldSectionTests
{
    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.WorldSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var sec = new WorldSection(bytes);

        var output = new byte[SaveWorkLayout.WorldSize];
        sec.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Raw_property_lengths_match_offset_table()
    {
        var sec = new WorldSection(new byte[SaveWorkLayout.WorldSize]);
        Assert.Equal(53, sec.TreasureFindDayRaw.Length);
        Assert.Equal(18, sec.UnregFindDayRaw.Length);
        Assert.Equal(108, sec.MoukeFinishDayRaw.Length);
        Assert.Equal(96, sec.MoukeDelayRaw.Length);
        Assert.Equal(200, sec.SnplInfRaw.Length);
        Assert.Equal(160, sec.SnplPageFlagRaw.Length);
        Assert.Equal(8, sec.SnplStaticFlagRaw.Length);
        Assert.Equal(64, sec.PersonYearRaw.Length);
        Assert.Equal(64, sec.MoukeEventRaw.Length);
        Assert.Equal(88, sec.WorldTrailingRaw.Length);
    }

    [Fact]
    public void TreasureFindDayRaw_reads_offset_0()
    {
        var bytes = new byte[SaveWorkLayout.WorldSize];
        bytes[0x00] = 0xAA;
        bytes[0x34] = 0xBB;
        var sec = new WorldSection(bytes);

        var raw = sec.TreasureFindDayRaw;
        Assert.Equal(0xAA, raw[0]);
        Assert.Equal(0xBB, raw[52]);
    }

    [Fact]
    public void SnplInfRaw_reads_offset_0x114()
    {
        var bytes = new byte[SaveWorkLayout.WorldSize];
        bytes[0x114] = 0x55;
        bytes[0x114 + 199] = 0x66;
        var sec = new WorldSection(bytes);

        var raw = sec.SnplInfRaw;
        Assert.Equal(0x55, raw[0]);
        Assert.Equal(0x66, raw[199]);
    }

    [Fact]
    public void Raw_properties_return_defensive_copies()
    {
        var sec = new WorldSection(new byte[SaveWorkLayout.WorldSize]);
        var first = sec.TreasureFindDayRaw;
        first[0] = 0xFF;
        var second = sec.TreasureFindDayRaw;
        Assert.Equal(0, second[0]);
    }
}

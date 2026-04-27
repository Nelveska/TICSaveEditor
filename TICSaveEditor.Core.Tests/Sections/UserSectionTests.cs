using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class UserSectionTests
{
    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.UserSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var sec = new UserSection(bytes);

        var output = new byte[SaveWorkLayout.UserSize];
        sec.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Raw_property_lengths_match_offset_table()
    {
        var sec = new UserSection(new byte[SaveWorkLayout.UserSize]);
        Assert.Equal(48, sec.GameProgressRaw.Length);
        Assert.Equal(32, sec.GameFlagRaw.Length);
        Assert.Equal(20, sec.BonusItemsRaw.Length);
    }

    [Fact]
    public void GameProgressRaw_reads_offset_0()
    {
        var bytes = new byte[SaveWorkLayout.UserSize];
        bytes[0x00] = 0x10;
        bytes[0x2F] = 0x20;
        var sec = new UserSection(bytes);

        var raw = sec.GameProgressRaw;
        Assert.Equal(0x10, raw[0]);
        Assert.Equal(0x20, raw[47]);
    }

    [Fact]
    public void Raw_properties_return_defensive_copies()
    {
        var sec = new UserSection(new byte[SaveWorkLayout.UserSize]);
        var first = sec.BonusItemsRaw;
        first[0] = 0xFF;
        var second = sec.BonusItemsRaw;
        Assert.Equal(0, second[0]);
    }
}

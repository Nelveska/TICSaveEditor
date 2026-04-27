using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class FftoAchievementSectionTests
{
    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.FftoAchievementSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var sec = new FftoAchievementSection(bytes);

        var output = new byte[SaveWorkLayout.FftoAchievementSize];
        sec.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Raw_property_lengths_match_offset_table()
    {
        var sec = new FftoAchievementSection(new byte[SaveWorkLayout.FftoAchievementSize]);
        Assert.Equal(50, sec.UnlockedRaw.Length);
        Assert.Equal(50, sec.ProgressRaw.Length);
        Assert.Equal(26, sec.PoachItemTypeRaw.Length);
        Assert.Equal(16, sec.SummonTypeRaw.Length);
        Assert.Equal(12, sec.GeomancyTypeRaw.Length);
        Assert.Equal(7, sec.SongTypeRaw.Length);
        Assert.Single(sec.IaidoTypeRaw);
        Assert.Equal(10, sec.DanceTurnsRaw.Length);
    }

    [Fact]
    public void UnlockedRaw_reads_offset_0()
    {
        var bytes = new byte[SaveWorkLayout.FftoAchievementSize];
        bytes[0x00] = 0x01;
        bytes[0x31] = 0x02;
        var sec = new FftoAchievementSection(bytes);

        var raw = sec.UnlockedRaw;
        Assert.Equal(0x01, raw[0]);
        Assert.Equal(0x02, raw[49]);
    }

    [Fact]
    public void DanceTurnsRaw_reads_offset_0xA2()
    {
        var bytes = new byte[SaveWorkLayout.FftoAchievementSize];
        bytes[0xA2] = 0xDE;
        bytes[0xAB] = 0xAD;
        var sec = new FftoAchievementSection(bytes);

        var raw = sec.DanceTurnsRaw;
        Assert.Equal(0xDE, raw[0]);
        Assert.Equal(0xAD, raw[9]);
    }

    [Fact]
    public void Raw_properties_return_defensive_copies()
    {
        var sec = new FftoAchievementSection(new byte[SaveWorkLayout.FftoAchievementSize]);
        var first = sec.UnlockedRaw;
        first[0] = 0xFF;
        var second = sec.UnlockedRaw;
        Assert.Equal(0, second[0]);
    }
}

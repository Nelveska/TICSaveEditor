using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class FftoBraveStorySectionTests
{
    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.FftoBraveStorySize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var sec = new FftoBraveStorySection(bytes);

        var output = new byte[SaveWorkLayout.FftoBraveStorySize];
        sec.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Raw_property_lengths_match_offset_table()
    {
        var sec = new FftoBraveStorySection(new byte[SaveWorkLayout.FftoBraveStorySize]);
        Assert.Equal(52, sec.ZodiacStoneRaw.Length);
        Assert.Equal(6, sec.BookRaw.Length);
        Assert.Equal(520, sec.JournalRaw.Length);
        Assert.Equal(440, sec.GlossaryRaw.Length);
        Assert.Equal(3, sec.WorldSituationRaw.Length);
        Assert.Equal(152, sec.BraveStoryTrailingRaw.Length);
    }

    [Fact]
    public void JournalRaw_reads_offset_0x3A()
    {
        var bytes = new byte[SaveWorkLayout.FftoBraveStorySize];
        bytes[0x3A] = 0x01;
        bytes[0x3A + 519] = 0x02;
        var sec = new FftoBraveStorySection(bytes);

        var raw = sec.JournalRaw;
        Assert.Equal(0x01, raw[0]);
        Assert.Equal(0x02, raw[519]);
    }

    [Fact]
    public void BraveStoryTrailingRaw_reads_last_152_bytes()
    {
        var bytes = new byte[SaveWorkLayout.FftoBraveStorySize];
        bytes[0x3FD] = 0x77;
        bytes[0x494] = 0x88;
        var sec = new FftoBraveStorySection(bytes);

        var raw = sec.BraveStoryTrailingRaw;
        Assert.Equal(0x77, raw[0]);
        Assert.Equal(0x88, raw[151]);
    }

    [Fact]
    public void Raw_properties_return_defensive_copies()
    {
        var sec = new FftoBraveStorySection(new byte[SaveWorkLayout.FftoBraveStorySize]);
        var first = sec.JournalRaw;
        first[0] = 0xFF;
        var second = sec.JournalRaw;
        Assert.Equal(0, second[0]);
    }
}

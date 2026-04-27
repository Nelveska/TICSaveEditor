using TICSaveEditor.Core.Records.Entries;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class FftoBattleSectionTests
{
    private static byte[] BlankBytes() => new byte[SaveWorkLayout.FftoBattleSize];

    [Fact]
    public void JobNewFlags_collection_has_21_entries()
    {
        var sec = new FftoBattleSection(BlankBytes());
        Assert.Equal(21, sec.JobNewFlags.Count);
    }

    [Fact]
    public void JobDisableFlags_collection_has_125_entries()
    {
        var sec = new FftoBattleSection(BlankBytes());
        Assert.Equal(125, sec.JobDisableFlags.Count);
    }

    [Fact]
    public void GetSet_JobNewFlag_writes_byte_at_correct_offset()
    {
        var sec = new FftoBattleSection(BlankBytes());
        sec.SetJobNewFlag(7, 0xAB);

        var output = new byte[SaveWorkLayout.FftoBattleSize];
        sec.WriteTo(output);

        // JobNew at section offset 0x00; index 7 → byte 0x07.
        Assert.Equal(0xAB, output[0x07]);
        Assert.Equal((byte)0xAB, sec.GetJobNewFlag(7));
    }

    [Fact]
    public void GetSet_JobDisableFlag_writes_byte_at_correct_offset()
    {
        var sec = new FftoBattleSection(BlankBytes());
        sec.SetJobDisableFlag(100, 0xCD);

        var output = new byte[SaveWorkLayout.FftoBattleSize];
        sec.WriteTo(output);

        // JobDisable at section offset 0x15; index 100 → byte 0x15 + 100 = 0x79.
        Assert.Equal(0xCD, output[0x79]);
    }

    [Fact]
    public void GuideArrivalFlagsRaw_returns_48_bytes_at_offset_0x92()
    {
        var bytes = BlankBytes();
        bytes[0x92] = 0x11;
        bytes[0xC1] = 0x22;
        var sec = new FftoBattleSection(bytes);

        var raw = sec.GuideArrivalFlagsRaw;
        Assert.Equal(48, raw.Length);
        Assert.Equal(0x11, raw[0]);
        Assert.Equal(0x22, raw[47]);
    }

    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.FftoBattleSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var sec = new FftoBattleSection(bytes);

        var output = new byte[SaveWorkLayout.FftoBattleSize];
        sec.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void SetJobNewFlag_out_of_range_throws()
    {
        var sec = new FftoBattleSection(BlankBytes());
        Assert.Throws<ArgumentOutOfRangeException>(() => sec.SetJobNewFlag(21, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sec.SetJobNewFlag(-1, 0));
    }

    [Fact]
    public void SetJobDisableFlag_out_of_range_throws()
    {
        var sec = new FftoBattleSection(BlankBytes());
        Assert.Throws<ArgumentOutOfRangeException>(() => sec.SetJobDisableFlag(125, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => sec.SetJobDisableFlag(-1, 0));
    }
}

using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class FftoWorldSectionTests
{
    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.FftoWorldSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var sec = new FftoWorldSection(bytes);

        var output = new byte[SaveWorkLayout.FftoWorldSize];
        sec.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void RawBytes_length_is_520()
    {
        var sec = new FftoWorldSection(new byte[SaveWorkLayout.FftoWorldSize]);
        Assert.Equal(520, sec.RawBytes.Length);
    }

    [Fact]
    public void RawBytes_returns_defensive_copy()
    {
        var bytes = new byte[SaveWorkLayout.FftoWorldSize];
        bytes[0] = 0x42;
        var sec = new FftoWorldSection(bytes);
        var first = sec.RawBytes;
        first[0] = 0xFF;
        var second = sec.RawBytes;
        Assert.Equal(0x42, second[0]);
    }
}

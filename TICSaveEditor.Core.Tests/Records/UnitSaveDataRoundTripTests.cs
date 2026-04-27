using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataRoundTripTests
{
    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(42);
        var bytes = new byte[600];
        rng.NextBytes(bytes);

        var unit = new UnitSaveData(bytes);
        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(bytes, output);
    }

    [Fact]
    public void Mutation_then_rewrite_preserves_unmutated_regions()
    {
        var rng = new Random(123);
        var bytes = new byte[600];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var unit = new UnitSaveData(bytes);
        unit.Level = 50;

        var output = new byte[600];
        unit.WriteTo(output);

        // Level lives at offset 0x1D.
        Assert.Equal(50, output[0x1D]);

        // Everything outside the Level byte should match pristine input.
        for (int i = 0; i < 600; i++)
        {
            if (i == 0x1D) continue;
            Assert.Equal(pristine[i], output[i]);
        }
    }

    [Fact]
    public void WriteTo_rejects_destination_smaller_than_600()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentException>(() => unit.WriteTo(new byte[599]));
    }

    [Fact]
    public void WriteTo_rejects_destination_larger_than_600()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentException>(() => unit.WriteTo(new byte[601]));
    }
}

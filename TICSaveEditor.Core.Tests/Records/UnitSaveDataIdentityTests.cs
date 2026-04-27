using System.Buffers.Binary;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataIdentityTests
{
    [Fact]
    public void Character_round_trips_at_offset_0x00()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x80;
        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x80, output[0x00]);
        Assert.Equal(0x80, unit.Character);
    }

    [Fact]
    public void Resist_round_trips_at_offset_0x01()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Resist = 0x05;
        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x05, output[0x01]);
    }

    [Fact]
    public void Job_round_trips_at_offset_0x02()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Job = 0x4B;
        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x4B, output[0x02]);
    }

    [Fact]
    public void Union_round_trips_at_offset_0x03()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Union = 0x09;
        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x09, output[0x03]);
    }

    [Fact]
    public void Reserved05_round_trips_at_offset_0x05_unchecked()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Reserved05 = 0xAB;
        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0xAB, output[0x05]);
    }

    [Fact]
    public void Zodiac_round_trips_at_offset_0x06()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Zodiac = 7;
        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(7, output[0x06]);
    }

    [Fact]
    public void Identity_u16_fields_use_little_endian_at_offsets_0x08_0x0A_0x0C()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.ReactionAbility = 0x1234;
        unit.SupportAbility = 0x5678;
        unit.MoveAbility = 0x9ABC;

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x08, 2)));
        Assert.Equal(0x5678, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x0A, 2)));
        Assert.Equal(0x9ABC, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x0C, 2)));
    }

    [Fact]
    public void Setting_Job_does_not_disturb_neighbor_bytes()
    {
        var rng = new Random(999);
        var bytes = new byte[600];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var unit = new UnitSaveData(bytes);
        unit.Job = 0x42;

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0x42, output[0x02]);
        for (int i = 0; i < 600; i++)
        {
            if (i == 0x02) continue;
            Assert.Equal(pristine[i], output[i]);
        }
    }
}

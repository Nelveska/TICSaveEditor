using System.Buffers.Binary;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataNameMetadataTests
{
    [Fact]
    public void ChrNameRaw_setter_throws_for_length_other_than_64()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentException>(() => unit.ChrNameRaw = new byte[63]);
        Assert.Throws<ArgumentException>(() => unit.ChrNameRaw = new byte[65]);
        Assert.Throws<ArgumentException>(() => unit.ChrNameRaw = Array.Empty<byte>());
    }

    [Fact]
    public void ChrNameRaw_returns_independent_copy()
    {
        var bytes = new byte[600];
        for (int i = 0; i < 64; i++) bytes[0xDC + i] = (byte)(0x40 + i);
        var unit = new UnitSaveData(bytes);

        var first = unit.ChrNameRaw;
        first[0] = 0xFF;

        var second = unit.ChrNameRaw;
        Assert.Equal(0x40, second[0]);
    }

    [Fact]
    public void ChrNameRaw_round_trips_64_bytes_verbatim()
    {
        var unit = new UnitSaveData(new byte[600]);
        var name = new byte[64];
        for (int i = 0; i < 64; i++) name[i] = (byte)i;
        unit.ChrNameRaw = name;

        var output = new byte[600];
        unit.WriteTo(output);
        for (int i = 0; i < 64; i++) Assert.Equal(i, output[0xDC + i]);
    }

    [Fact]
    public void Slot_metadata_bytes_round_trip_at_offsets_0x11C_through_0x125()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.NameNo = 0xABCD;
        unit.InTrip = 1;
        unit.Parasite = 2;
        unit.EggColor = 3;
        unit.PspKilledNum = 4;
        unit.UnitOrderId = 5;
        unit.UnitStartingTeam = 6;
        unit.UnitJoinId = 7;
        unit.CurrentEquipSetNumber = 2;

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0xABCD, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x11C, 2)));
        Assert.Equal(1, output[0x11E]);
        Assert.Equal(2, output[0x11F]);
        Assert.Equal(3, output[0x120]);
        Assert.Equal(4, output[0x121]);
        Assert.Equal(5, output[0x122]);
        Assert.Equal(6, output[0x123]);
        Assert.Equal(7, output[0x124]);
        Assert.Equal(2, output[0x125]);
    }

    [Fact]
    public void Trailing_Pad2_and_CharaNameKey_round_trip_verbatim()
    {
        var rng = new Random(7);
        var bytes = new byte[600];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var unit = new UnitSaveData(bytes);
        unit.CharaNameKey = 0x1234;

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x230, 2)));
        // Pad (0x22E..0x22F) and Pad2 (0x232..0x257) preserved.
        for (int i = 0x22E; i < 0x230; i++) Assert.Equal(pristine[i], output[i]);
        for (int i = 0x232; i < 0x258; i++) Assert.Equal(pristine[i], output[i]);
    }
}

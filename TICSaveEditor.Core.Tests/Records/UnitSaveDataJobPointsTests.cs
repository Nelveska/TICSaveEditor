using System.Buffers.Binary;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataJobPointsTests
{
    [Fact]
    public void GetJobPoint_throws_on_index_outside_0_through_22()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.GetJobPoint(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.GetJobPoint(23));
    }

    [Fact]
    public void SetJobPoint_round_trips_u16_LE_at_offset_0x80_plus_2_times_index()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.SetJobPoint(0, 100);     // Squire at 0x80
        unit.SetJobPoint(3, 999);     // Archer at 0x86
        unit.SetJobPoint(22, 0xFFFF); // reserved at 0xAC

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(100, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x80, 2)));
        Assert.Equal(999, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x86, 2)));
        Assert.Equal(0xFFFF, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0xAC, 2)));
    }

    [Fact]
    public void JobPoint_slot_22_is_preserved_through_round_trip()
    {
        var bytes = new byte[600];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0xAC, 2), 0x1234);
        var unit = new UnitSaveData(bytes);

        Assert.Equal(0x1234, unit.GetJobPoint(22));

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0xAC, 2)));
    }

    [Fact]
    public void JobPoints_collection_has_exactly_23_entries()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(23, unit.JobPoints.Count);
        for (int i = 0; i < 23; i++) Assert.Equal(i, unit.JobPoints[i].JobId);
    }
}

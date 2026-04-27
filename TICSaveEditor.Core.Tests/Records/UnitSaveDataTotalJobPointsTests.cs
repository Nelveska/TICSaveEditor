using System.Buffers.Binary;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataTotalJobPointsTests
{
    [Fact]
    public void GetTotalJobPoint_throws_on_index_outside_0_through_22()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.GetTotalJobPoint(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.GetTotalJobPoint(23));
    }

    [Fact]
    public void SetTotalJobPoint_round_trips_u16_LE_at_offset_0xAE_plus_2_times_index()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.SetTotalJobPoint(0, 100);
        unit.SetTotalJobPoint(15, 9999);
        unit.SetTotalJobPoint(22, 0xFFFF);

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(100, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0xAE, 2)));
        Assert.Equal(9999, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0xAE + 30, 2)));
        Assert.Equal(0xFFFF, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0xDA, 2)));
    }

    [Fact]
    public void TotalJobPoints_collection_has_exactly_23_entries()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(23, unit.TotalJobPoints.Count);
        for (int i = 0; i < 23; i++) Assert.Equal(i, unit.TotalJobPoints[i].JobId);
    }
}

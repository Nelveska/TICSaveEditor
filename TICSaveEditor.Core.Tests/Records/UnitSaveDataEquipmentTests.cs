using System.Buffers.Binary;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataEquipmentTests
{
    [Fact]
    public void GetEquipItem_throws_for_negative_index()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.GetEquipItem(-1));
    }

    [Fact]
    public void GetEquipItem_throws_for_index_greater_than_6()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Throws<ArgumentOutOfRangeException>(() => unit.GetEquipItem(7));
    }

    [Fact]
    public void SetEquipItem_round_trips_u16_at_correct_offset()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.SetEquipItem(0, 0x1234);
        unit.SetEquipItem(3, 0x5678);
        unit.SetEquipItem(6, 0x9ABC);

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0x1234, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x0E, 2)));
        Assert.Equal(0x5678, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x14, 2)));
        Assert.Equal(0x9ABC, BinaryPrimitives.ReadUInt16LittleEndian(output.AsSpan(0x1A, 2)));
    }

    [Fact]
    public void EmptyEquipSlotSentinel_constant_is_0x00FF()
    {
        Assert.Equal(0x00FF, UnitSaveData.EmptyEquipSlotSentinel);
    }

    [Fact]
    public void Setting_equipment_does_not_disturb_neighbor_slots()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.SetEquipItem(0, 0x009D);
        unit.SetEquipItem(1, 0x00FF);
        unit.SetEquipItem(2, 0xABCD);

        unit.SetEquipItem(1, 0x1234);

        Assert.Equal(0x009D, unit.GetEquipItem(0));
        Assert.Equal(0x1234, unit.GetEquipItem(1));
        Assert.Equal(0xABCD, unit.GetEquipItem(2));
    }
}

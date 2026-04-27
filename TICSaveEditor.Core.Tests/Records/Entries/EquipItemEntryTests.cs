using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records.Entries;

public class EquipItemEntryTests
{
    [Fact]
    public void Slot_property_derives_from_Index()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(EquipmentSlot.Head, unit.EquipItems[0].Slot);
        Assert.Equal(EquipmentSlot.Body, unit.EquipItems[1].Slot);
        Assert.Equal(EquipmentSlot.Accessory, unit.EquipItems[2].Slot);
        Assert.Equal(EquipmentSlot.RightWeapon, unit.EquipItems[3].Slot);
        Assert.Equal(EquipmentSlot.RightShield, unit.EquipItems[4].Slot);
        Assert.Equal(EquipmentSlot.LeftWeapon, unit.EquipItems[5].Slot);
        Assert.Equal(EquipmentSlot.LeftShield, unit.EquipItems[6].Slot);
    }

    [Fact]
    public void IsEmpty_true_when_value_equals_sentinel()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.SetEquipItem(0, UnitSaveData.EmptyEquipSlotSentinel);
        Assert.True(unit.EquipItems[0].IsEmpty);

        unit.SetEquipItem(0, 0x1234);
        Assert.False(unit.EquipItems[0].IsEmpty);
    }

    [Fact]
    public void Setting_value_routes_through_owner_SetEquipItem()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.EquipItems[2].Value = 0xABCD;
        Assert.Equal(0xABCD, unit.GetEquipItem(2));
    }
}

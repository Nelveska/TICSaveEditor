using System.Runtime.InteropServices;
using TICSaveEditor.Core.Records.Layouts;

namespace TICSaveEditor.Core.Tests.Records.Layouts;

public class UnitSaveDataLayoutTests
{
    [Fact]
    public void UnitSaveDataLayout_size_is_600_bytes()
    {
        Assert.Equal(600, Marshal.SizeOf<UnitSaveDataLayout>());
    }

    [Fact]
    public void EquipSetLayout_size_is_88_bytes()
    {
        Assert.Equal(88, Marshal.SizeOf<EquipSetLayout>());
    }

    [Fact]
    public void EquipSetLayout_Name_is_at_offset_0()
    {
        Assert.Equal(0, (int)Marshal.OffsetOf<EquipSetLayout>(nameof(EquipSetLayout.Name)));
    }

    [Fact]
    public void EquipSetLayout_ItemBytes_is_at_offset_0x42()
    {
        Assert.Equal(0x42, (int)Marshal.OffsetOf<EquipSetLayout>(nameof(EquipSetLayout.ItemBytes)));
    }

    [Fact]
    public void EquipSetLayout_AbilityBytes_is_at_offset_0x4C()
    {
        Assert.Equal(0x4C, (int)Marshal.OffsetOf<EquipSetLayout>(nameof(EquipSetLayout.AbilityBytes)));
    }

    [Fact]
    public void EquipSetLayout_Job_is_at_offset_0x56()
    {
        Assert.Equal(0x56, (int)Marshal.OffsetOf<EquipSetLayout>(nameof(EquipSetLayout.Job)));
    }

    [Fact]
    public void EquipSetLayout_IsDoubleHand_is_at_offset_0x57()
    {
        Assert.Equal(0x57, (int)Marshal.OffsetOf<EquipSetLayout>(nameof(EquipSetLayout.IsDoubleHand)));
    }
}

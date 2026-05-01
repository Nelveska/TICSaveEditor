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
    public void CombatSetLayout_size_is_88_bytes()
    {
        Assert.Equal(88, Marshal.SizeOf<CombatSetLayout>());
    }

    [Fact]
    public void CombatSetLayout_Name_is_at_offset_0()
    {
        Assert.Equal(0, (int)Marshal.OffsetOf<CombatSetLayout>(nameof(CombatSetLayout.Name)));
    }

    [Fact]
    public void CombatSetLayout_NamePadding_is_at_offset_0x10()
    {
        Assert.Equal(0x10, (int)Marshal.OffsetOf<CombatSetLayout>(nameof(CombatSetLayout.NamePadding)));
    }

    [Fact]
    public void CombatSetLayout_ItemBytes_is_at_offset_0x42()
    {
        Assert.Equal(0x42, (int)Marshal.OffsetOf<CombatSetLayout>(nameof(CombatSetLayout.ItemBytes)));
    }

    [Fact]
    public void CombatSetLayout_Skillsets_is_at_offset_0x4C()
    {
        Assert.Equal(0x4C, (int)Marshal.OffsetOf<CombatSetLayout>(nameof(CombatSetLayout.Skillsets)));
    }

    [Fact]
    public void CombatSetLayout_Abilities_is_at_offset_0x50()
    {
        Assert.Equal(0x50, (int)Marshal.OffsetOf<CombatSetLayout>(nameof(CombatSetLayout.Abilities)));
    }

    [Fact]
    public void CombatSetLayout_Job_is_at_offset_0x56()
    {
        Assert.Equal(0x56, (int)Marshal.OffsetOf<CombatSetLayout>(nameof(CombatSetLayout.Job)));
    }

    [Fact]
    public void CombatSetLayout_IsDoubleHand_is_at_offset_0x57()
    {
        Assert.Equal(0x57, (int)Marshal.OffsetOf<CombatSetLayout>(nameof(CombatSetLayout.IsDoubleHand)));
    }
}

using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Records.Entries;

namespace TICSaveEditor.Core.Tests.Records.Entries;

public class JobAbilityFlagsEntryTests
{
    [Fact]
    public void RawBytes_returns_3_byte_independent_copy()
    {
        var bytes = new byte[600];
        bytes[0x32] = 0x11; // Squire byte 0
        bytes[0x33] = 0x22;
        bytes[0x34] = 0x33;
        var unit = new UnitSaveData(bytes);

        var raw = unit.AbilityFlags[0].RawBytes;
        Assert.Equal(3, raw.Length);
        Assert.Equal(0x11, raw[0]);
        Assert.Equal(0x22, raw[1]);
        Assert.Equal(0x33, raw[2]);

        // Mutating the returned array does not affect the entry.
        raw[0] = 0xFF;
        Assert.Equal(0x11, unit.AbilityFlags[0].RawBytes[0]);
    }

    [Fact]
    public void IsLearned_throws_on_index_below_0_or_above_23()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.AbilityFlags[0];
        Assert.Throws<ArgumentOutOfRangeException>(() => entry.IsLearned(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => entry.IsLearned(24));
    }

    [Fact]
    public void SetLearned_bit_0_lights_byte_0_bit_0()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.AbilityFlags[3].SetLearned(0, true);

        var raw = unit.AbilityFlags[3].RawBytes;
        Assert.Equal(0x01, raw[0]);
        Assert.Equal(0x00, raw[1]);
        Assert.Equal(0x00, raw[2]);
    }

    [Fact]
    public void SetLearned_bit_15_lights_byte_1_bit_7()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.AbilityFlags[3].SetLearned(15, true);

        var raw = unit.AbilityFlags[3].RawBytes;
        Assert.Equal(0x00, raw[0]);
        Assert.Equal(0x80, raw[1]);
        Assert.Equal(0x00, raw[2]);
    }

    [Fact]
    public void SetLearned_bit_23_lights_byte_2_bit_7()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.AbilityFlags[3].SetLearned(23, true);

        var raw = unit.AbilityFlags[3].RawBytes;
        Assert.Equal(0x00, raw[0]);
        Assert.Equal(0x00, raw[1]);
        Assert.Equal(0x80, raw[2]);
    }

    [Fact]
    public void IsActiveLearned_routes_to_IsLearned_at_index_0_through_15()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.AbilityFlags[5];
        entry.SetLearned(7, true);
        Assert.True(entry.IsActiveLearned(7));
        Assert.False(entry.IsActiveLearned(8));
    }

    [Fact]
    public void IsPassiveLearned_routes_to_IsLearned_at_index_16_plus_passive_index()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.AbilityFlags[5];
        entry.SetLearned(16, true);
        Assert.True(entry.IsPassiveLearned(0));
        entry.SetPassiveLearned(7, true);
        Assert.True(entry.IsLearned(23));
    }

    [Fact]
    public void LearnAll_then_AllLearned_is_true_and_NoneLearned_is_false()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.AbilityFlags[10];
        entry.LearnAll();
        Assert.True(entry.AllLearned);
        Assert.False(entry.NoneLearned);
        entry.ForgetAll();
        Assert.False(entry.AllLearned);
        Assert.True(entry.NoneLearned);
    }
}

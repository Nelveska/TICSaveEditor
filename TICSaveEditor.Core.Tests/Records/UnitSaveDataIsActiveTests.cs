using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

/// <summary>
/// Exercises <see cref="UnitSaveData.IsInActiveParty"/>. Per
/// <c>decisions_unit_index_active_flag.md</c>: active in current party iff
/// <c>Character != 0 AND Resist == ownSlotIndex</c>; otherwise inactive.
/// </summary>
public class UnitSaveDataIsActiveTests
{
    [Fact]
    public void Empty_unit_is_not_in_active_party()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.False(unit.IsInActiveParty(0));
        Assert.False(unit.IsInActiveParty(51));
    }

    [Fact]
    public void Populated_unit_with_resist_matching_slot_is_active()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x80;     // generic male recruit
        unit.Resist = 5;
        Assert.True(unit.IsInActiveParty(5));
    }

    [Fact]
    public void Populated_unit_with_resist_0xFF_is_inactive_even_when_character_is_set()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x07;     // departed guest pattern (Argath)
        unit.Resist = 0xFF;
        Assert.False(unit.IsInActiveParty(51));
    }

    [Fact]
    public void Populated_unit_with_mismatched_resist_is_inactive()
    {
        // Edge: corrupt save where resist is some non-0xFF, non-own-index value.
        // Treat as inactive defensively per decisions_unit_index_active_flag.md.
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x80;
        unit.Resist = 0xAB;
        Assert.False(unit.IsInActiveParty(5));
    }

    [Fact]
    public void Active_to_inactive_transition_via_Resist_setter()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x07;
        unit.Resist = 0x33;          // 51 — active guest
        Assert.True(unit.IsInActiveParty(51));

        unit.Resist = 0xFF;          // simulate the Argath departure transition
        Assert.False(unit.IsInActiveParty(51));
    }
}

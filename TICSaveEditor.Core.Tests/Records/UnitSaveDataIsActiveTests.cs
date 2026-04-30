using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

/// <summary>
/// Exercises <see cref="UnitSaveData.IsInActiveParty"/>. Per
/// <c>decisions_unit_index_active_flag.md</c>: active in current party iff
/// <c>Character != 0 AND UnitIndex == ownSlotIndex</c>; otherwise inactive.
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
    public void Populated_unit_with_UnitIndex_matching_slot_is_active()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x80;     // generic male recruit
        unit.UnitIndex = 5;
        Assert.True(unit.IsInActiveParty(5));
    }

    [Fact]
    public void Populated_unit_with_UnitIndex_0xFF_is_inactive_even_when_character_is_set()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x07;     // departed guest pattern (Argath)
        unit.UnitIndex = 0xFF;
        Assert.False(unit.IsInActiveParty(51));
    }

    [Fact]
    public void Populated_unit_with_mismatched_UnitIndex_is_inactive()
    {
        // Edge: corrupt save where UnitIndex is some non-0xFF, non-own-index value.
        // Treat as inactive defensively per decisions_unit_index_active_flag.md.
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x80;
        unit.UnitIndex = 0xAB;
        Assert.False(unit.IsInActiveParty(5));
    }

    [Fact]
    public void Active_to_inactive_transition_via_UnitIndex_setter()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Character = 0x07;
        unit.UnitIndex = 0x33;          // 51 — active guest
        Assert.True(unit.IsInActiveParty(51));

        unit.UnitIndex = 0xFF;          // simulate the Argath departure transition
        Assert.False(unit.IsInActiveParty(51));
    }
}

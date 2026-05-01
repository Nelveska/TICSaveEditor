using System.ComponentModel;
using System.Text;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class CombatSetTests
{
    [Fact]
    public void Index_reflects_position_in_collection()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(0, unit.CombatSets[0].Index);
        Assert.Equal(1, unit.CombatSets[1].Index);
        Assert.Equal(2, unit.CombatSets[2].Index);
    }

    [Fact]
    public void Name_round_trips_ASCII_string()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[1].Name = "Slot1Preset";
        Assert.Equal("Slot1Preset", unit.CombatSets[1].Name);
    }

    [Fact]
    public void Name_decodes_up_to_first_null_byte()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Name = "Foo";

        var output = new byte[600];
        unit.WriteTo(output);

        // CombatSet0 starts at unit-relative offset 0x126; Name occupies bytes [0..65].
        // "Foo\0" should be at the very start; remainder beyond null is irrelevant.
        Assert.Equal((byte)'F', output[0x126]);
        Assert.Equal((byte)'o', output[0x127]);
        Assert.Equal((byte)'o', output[0x128]);
        Assert.Equal((byte)0, output[0x129]);
    }

    [Fact]
    public void Name_clamps_at_16_chars_no_null_terminator()
    {
        // Per the 2026-05-01 community-decomposition resolution (CombatSetLongName fixture),
        // the preset name is a fixed 16-byte ASCII field at CS+0x00..0x0F with no null
        // terminator within the field. NamePadding[50] at CS+0x10..0x41 is preserved as
        // opaque (we do NOT touch it on a name write).
        //
        // Sentinel test: pre-populate NamePadding[0] with 0xAB, then write a 20-char name.
        // Assert: stored name == first 16 chars; byte at CS+0x0F is the 16th 'A' (NOT null);
        // byte at CS+0x10 is still 0xAB (padding preserved).
        var bytes = new byte[600];
        bytes[0x126 + 0x10] = 0xAB; // NamePadding[0] sentinel
        var unit = new UnitSaveData(bytes);

        var oversize = new string('A', 20);
        unit.CombatSets[0].Name = oversize;

        var stored = unit.CombatSets[0].Name;
        Assert.Equal(16, stored.Length);
        Assert.Equal(new string('A', 16), stored);

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal((byte)'A', output[0x126 + 0x0F]); // last byte of name = ASCII, not null
        Assert.Equal(0xAB, output[0x126 + 0x10]);     // padding sentinel preserved
    }

    [Fact]
    public void Name_setter_zero_pads_unused_bytes_within_name_field()
    {
        // Stale-byte hygiene: writing a shorter name after a longer one must zero-fill the
        // tail of Name[16] so leftover bytes from the previous name don't leak.
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Name = "TankBuild";   // 9 chars
        unit.CombatSets[0].Name = "Mage";        // 4 chars

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal((byte)'M', output[0x126 + 0]);
        Assert.Equal((byte)'a', output[0x126 + 1]);
        Assert.Equal((byte)'g', output[0x126 + 2]);
        Assert.Equal((byte)'e', output[0x126 + 3]);
        for (int i = 4; i < 16; i++)
            Assert.Equal((byte)0, output[0x126 + i]);
    }

    [Fact]
    public void Name_setter_handles_null_input_as_empty_string()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Name = "Existing";
        unit.CombatSets[0].Name = null!;
        Assert.Equal(string.Empty, unit.CombatSets[0].Name);
    }

    [Fact]
    public void Job_setter_round_trips()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[2].Job = 0x53;
        Assert.Equal(0x53, unit.CombatSets[2].Job);
    }

    [Fact]
    public void Job_mutation_writes_to_correct_byte_offset()
    {
        // CombatSet1 starts at unit-relative offset 0x126 + 88 = 0x17E.
        // Job lives at section-relative offset 0x56 → unit-relative 0x17E + 0x56 = 0x1D4.
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[1].Job = 0xAB;

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0xAB, output[0x126 + 88 + 0x56]);
    }

    [Fact]
    public void IsDoubleHand_round_trips_as_bool()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.False(unit.CombatSets[0].IsDoubleHand);

        unit.CombatSets[0].IsDoubleHand = true;
        Assert.True(unit.CombatSets[0].IsDoubleHand);

        unit.CombatSets[0].IsDoubleHand = false;
        Assert.False(unit.CombatSets[0].IsDoubleHand);
    }

    [Fact]
    public void IsDoubleHand_writes_to_correct_byte_offset()
    {
        // CombatSet2 starts at 0x126 + 176 = 0x1D6; IsDoubleHand at section-relative 0x57.
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[2].IsDoubleHand = true;

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal((byte)1, output[0x126 + 176 + 0x57]);
    }

    [Fact]
    public void Setting_Job_fires_PropertyChanged_on_the_entry()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.CombatSets[1];

        var fired = new List<string>();
        ((INotifyPropertyChanged)entry).PropertyChanged +=
            (_, e) => fired.Add(e.PropertyName ?? string.Empty);

        entry.Job = 7;

        Assert.Contains(nameof(CombatSet.Job), fired);
    }

    [Fact]
    public void Skillset0_round_trips_signed_short()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Skillset0 = 0x1234;
        Assert.Equal(0x1234, unit.CombatSets[0].Skillset0);

        unit.CombatSets[0].Skillset0 = -1;
        Assert.Equal(-1, unit.CombatSets[0].Skillset0);
    }

    [Fact]
    public void Skillset1_round_trips_signed_short()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[2].Skillset1 = 0x0E;
        Assert.Equal(0x0E, unit.CombatSets[2].Skillset1);
    }

    [Fact]
    public void ReactionAbility_round_trips_unsigned_short()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[1].ReactionAbility = 0xABCD;
        Assert.Equal(0xABCD, unit.CombatSets[1].ReactionAbility);
    }

    [Fact]
    public void SupportAbility_round_trips_unsigned_short()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[1].SupportAbility = 0x00FF;
        Assert.Equal(0x00FF, unit.CombatSets[1].SupportAbility);
    }

    [Fact]
    public void MovementAbility_round_trips_unsigned_short()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[2].MovementAbility = 0xE7;
        Assert.Equal(0xE7, unit.CombatSets[2].MovementAbility);
    }

    [Fact]
    public void Skillset0_writes_LE_bytes_at_section_relative_0x4C()
    {
        // CS1 starts at unit-relative 0x126 + 88 = 0x17E.
        // Skillset0 occupies 0x4C..0x4D within the preset → unit-relative 0x1CA..0x1CB.
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[1].Skillset0 = unchecked((short)0xBEEF);

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0xEF, output[0x126 + 88 + 0x4C]);
        Assert.Equal(0xBE, output[0x126 + 88 + 0x4D]);
    }

    [Fact]
    public void Skillset1_writes_LE_bytes_at_section_relative_0x4E()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Skillset1 = 0x1122;

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x22, output[0x126 + 0x4E]);
        Assert.Equal(0x11, output[0x126 + 0x4F]);
    }

    [Fact]
    public void ReactionAbility_writes_LE_bytes_at_section_relative_0x50()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].ReactionAbility = 0x1234;

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x34, output[0x126 + 0x50]);
        Assert.Equal(0x12, output[0x126 + 0x51]);
    }

    [Fact]
    public void SupportAbility_writes_LE_bytes_at_section_relative_0x52()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].SupportAbility = 0x5678;

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x78, output[0x126 + 0x52]);
        Assert.Equal(0x56, output[0x126 + 0x53]);
    }

    [Fact]
    public void MovementAbility_writes_LE_bytes_at_section_relative_0x54()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].MovementAbility = 0x9ABC;

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0xBC, output[0x126 + 0x54]);
        Assert.Equal(0x9A, output[0x126 + 0x55]);
    }

    [Fact]
    public void Skillset_and_Ability_typed_reads_decode_existing_bytes_correctly()
    {
        // Pre-populate raw bytes at CS0+0x4C..0x55, then verify each typed property
        // decodes its expected u16/i16 LE slice.
        var bytes = new byte[600];
        // CS0+0x4C..0x4D: Skillset0 = 0x0102
        bytes[0x126 + 0x4C] = 0x02;
        bytes[0x126 + 0x4D] = 0x01;
        // CS0+0x4E..0x4F: Skillset1 = -1 (0xFFFF)
        bytes[0x126 + 0x4E] = 0xFF;
        bytes[0x126 + 0x4F] = 0xFF;
        // CS0+0x50..0x51: ReactionAbility = 0x0708
        bytes[0x126 + 0x50] = 0x08;
        bytes[0x126 + 0x51] = 0x07;
        // CS0+0x52..0x53: SupportAbility = 0x0009
        bytes[0x126 + 0x52] = 0x09;
        bytes[0x126 + 0x53] = 0x00;
        // CS0+0x54..0x55: MovementAbility = 0x000A
        bytes[0x126 + 0x54] = 0x0A;
        bytes[0x126 + 0x55] = 0x00;

        var unit = new UnitSaveData(bytes);
        Assert.Equal(0x0102, unit.CombatSets[0].Skillset0);
        Assert.Equal(-1, unit.CombatSets[0].Skillset1);
        Assert.Equal(0x0708, unit.CombatSets[0].ReactionAbility);
        Assert.Equal(0x0009, unit.CombatSets[0].SupportAbility);
        Assert.Equal(0x000A, unit.CombatSets[0].MovementAbility);
    }

    [Fact]
    public void Setting_Skillset0_fires_PropertyChanged_on_the_entry()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.CombatSets[1];

        var fired = new List<string>();
        ((INotifyPropertyChanged)entry).PropertyChanged +=
            (_, e) => fired.Add(e.PropertyName ?? string.Empty);

        entry.Skillset0 = 0x7A;

        Assert.Contains(nameof(CombatSet.Skillset0), fired);
    }

    [Fact]
    public void Setting_MovementAbility_fires_PropertyChanged_on_the_entry()
    {
        var unit = new UnitSaveData(new byte[600]);
        var entry = unit.CombatSets[2];

        var fired = new List<string>();
        ((INotifyPropertyChanged)entry).PropertyChanged +=
            (_, e) => fired.Add(e.PropertyName ?? string.Empty);

        entry.MovementAbility = 0xE6;

        Assert.Contains(nameof(CombatSet.MovementAbility), fired);
    }

    [Fact]
    public void Setting_typed_accessor_to_existing_value_does_not_fire()
    {
        // Idempotent setter: same-value writes should be a no-op (mirrors the existing
        // SetCombatSetJob / SetCombatSetIsDoubleHand idempotent-setter contract).
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].ReactionAbility = 0x42;

        var entry = unit.CombatSets[0];
        var fired = new List<string>();
        ((INotifyPropertyChanged)entry).PropertyChanged +=
            (_, e) => fired.Add(e.PropertyName ?? string.Empty);

        entry.ReactionAbility = 0x42; // same value
        Assert.DoesNotContain(nameof(CombatSet.ReactionAbility), fired);
    }
}

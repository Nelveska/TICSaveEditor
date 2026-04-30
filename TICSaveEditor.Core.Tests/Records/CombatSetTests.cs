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
    public void Name_clamps_at_65_chars_and_writes_null_terminator()
    {
        var unit = new UnitSaveData(new byte[600]);
        var oversize = new string('A', 100);
        unit.CombatSets[0].Name = oversize;

        // Stored value should be 65 'A's (one byte short of the 66-byte buffer for the null).
        var stored = unit.CombatSets[0].Name;
        Assert.Equal(65, stored.Length);
        Assert.Equal(new string('A', 65), stored);

        var output = new byte[600];
        unit.WriteTo(output);
        // The 66th byte (offset 0x126 + 65) should be the null terminator if bytesWritten < buffer length.
        // bytesWritten == 65 here, which is < 66, so dest[65] = 0.
        Assert.Equal((byte)0, output[0x126 + 65]);
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
    public void RawItemBytes_returns_defensive_copy_of_correct_length()
    {
        var bytes = new byte[600];
        for (int i = 0; i < 10; i++) bytes[0x126 + 0x42 + i] = (byte)(0x10 + i);
        var unit = new UnitSaveData(bytes);

        var copy = unit.CombatSets[0].RawItemBytes;
        Assert.Equal(10, copy.Length);
        for (int i = 0; i < 10; i++) Assert.Equal((byte)(0x10 + i), copy[i]);

        // Mutate the copy and confirm the underlying bytes are unaffected.
        copy[0] = 0xFF;
        Assert.Equal(0x10, unit.CombatSets[0].RawItemBytes[0]);
    }

    [Fact]
    public void RawAbilityBytes_returns_defensive_copy_at_correct_offset()
    {
        var bytes = new byte[600];
        for (int i = 0; i < 10; i++) bytes[0x126 + 0x4C + i] = (byte)(0xA0 + i);
        var unit = new UnitSaveData(bytes);

        var copy = unit.CombatSets[0].RawAbilityBytes;
        Assert.Equal(10, copy.Length);
        for (int i = 0; i < 10; i++) Assert.Equal((byte)(0xA0 + i), copy[i]);
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
}

using System.ComponentModel;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataCombatSetIntegrationTests
{
    [Fact]
    public void CombatSets_collection_has_exactly_three_entries()
    {
        var unit = new UnitSaveData(new byte[600]);
        Assert.Equal(3, unit.CombatSets.Count);
    }

    [Fact]
    public void CombatSets_returns_stable_references_across_calls()
    {
        var unit = new UnitSaveData(new byte[600]);
        var firstCall = unit.CombatSets[1];
        var secondCall = unit.CombatSets[1];
        Assert.Same(firstCall, secondCall);
    }

    [Fact]
    public void Each_CombatSet_writes_to_distinct_unit_relative_offsets()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Job = 0x10;
        unit.CombatSets[1].Job = 0x20;
        unit.CombatSets[2].Job = 0x30;

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal(0x10, output[0x126 + 0x56]);
        Assert.Equal(0x20, output[0x126 + 88 + 0x56]);
        Assert.Equal(0x30, output[0x126 + 176 + 0x56]);
    }

    [Fact]
    public void Mutating_one_CombatSet_does_not_affect_others()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Name = "First";
        unit.CombatSets[1].Name = "Second";

        Assert.Equal("First", unit.CombatSets[0].Name);
        Assert.Equal("Second", unit.CombatSets[1].Name);
        Assert.Equal(string.Empty, unit.CombatSets[2].Name);
    }

    [Fact]
    public void Setting_CombatSet_field_outside_suspend_scope_fires_on_entry_not_unit()
    {
        // Mirrors the M4 EquipItems contract: outside SuspendNotifications, entry
        // INPC fires on the entry; the unit-level "CombatSets" event is reserved
        // for suspend-scope coalescing (see NotifyOrQueue in UnitSaveData).
        var unit = new UnitSaveData(new byte[600]);

        var unitEvents = new List<string>();
        var entryEvents = new List<string>();
        ((INotifyPropertyChanged)unit).PropertyChanged +=
            (_, e) => unitEvents.Add(e.PropertyName ?? string.Empty);
        ((INotifyPropertyChanged)unit.CombatSets[0]).PropertyChanged +=
            (_, e) => entryEvents.Add(e.PropertyName ?? string.Empty);

        unit.CombatSets[0].Job = 5;

        Assert.DoesNotContain(nameof(UnitSaveData.CombatSets), unitEvents);
        Assert.Contains(nameof(CombatSet.Job), entryEvents);
    }

    [Fact]
    public void CombatSet_round_trips_byte_identical_when_no_mutations()
    {
        var rng = new Random(7);
        var bytes = new byte[600];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var unit = new UnitSaveData(bytes);

        // Read every public surface — must not mutate underlying bytes.
        for (int i = 0; i < 3; i++)
        {
            _ = unit.CombatSets[i].Name;
            _ = unit.CombatSets[i].Job;
            _ = unit.CombatSets[i].IsDoubleHand;
            _ = unit.CombatSets[i].RawItemBytes;
            _ = unit.CombatSets[i].RawAbilityBytes;
        }

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Real_fixture_CombatSet_round_trip_byte_identical_no_mutation()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", "EquipSet", "enhanced.png");
        if (!File.Exists(path))
        {
            // Fixture not provided in this checkout — skip.
            return;
        }

        var sourceBytes = File.ReadAllBytes(path);
        var save = TICSaveEditor.Core.Save.SaveFileLoader.Load(sourceBytes, path);
        var tempPath = Path.GetTempFileName() + ".png";
        try
        {
            save.SaveAs(tempPath);
            var roundTripped = File.ReadAllBytes(tempPath);
            Assert.Equal(sourceBytes, roundTripped);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void CombatSet_Name_writes_match_byte_layout_at_unit_relative_offset_0x126()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[0].Name = "ABC";
        unit.CombatSets[1].Name = "XY";

        var output = new byte[600];
        unit.WriteTo(output);

        Assert.Equal((byte)'A', output[0x126]);
        Assert.Equal((byte)'B', output[0x127]);
        Assert.Equal((byte)'C', output[0x128]);
        Assert.Equal((byte)0,   output[0x129]);

        // CombatSet1 starts at 0x126 + 88 = 0x17E
        Assert.Equal((byte)'X', output[0x17E]);
        Assert.Equal((byte)'Y', output[0x17F]);
        Assert.Equal((byte)0,   output[0x180]);
    }
}

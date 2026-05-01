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
            _ = unit.CombatSets[i].Skillset0;
            _ = unit.CombatSets[i].Skillset1;
            _ = unit.CombatSets[i].ReactionAbility;
            _ = unit.CombatSets[i].SupportAbility;
            _ = unit.CombatSets[i].MovementAbility;
        }

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Real_fixture_CombatSet_round_trip_byte_identical_no_mutation()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", "Baseline", "enhanced.png");
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
    public void Real_fixture_ChangeOneSkillset_decodes_Skillset1_via_typed_accessor()
    {
        // Per the 2026-05-01 SaveDiff diagnostic, the ChangeOneSkillset fixture's
        // single-edit landed at CS0+0x4E (= Skillset1 low byte): Ramza's secondary-
        // action skillset shifted 0x09 -> 0x0E. Baseline still reads 0x09.
        // This is the typed-accessor end-to-end smoke for Phase 1.
        var baselinePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", "Baseline", "enhanced.png");
        var variantPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", "ChangeOneSkillset", "enhanced.png");
        if (!File.Exists(baselinePath) || !File.Exists(variantPath))
        {
            // Fixtures not provided in this checkout — skip.
            return;
        }

        var baseline = (TICSaveEditor.Core.Save.ManualSaveFile)
            TICSaveEditor.Core.Save.SaveFileLoader.Load(File.ReadAllBytes(baselinePath), baselinePath);
        var variant = (TICSaveEditor.Core.Save.ManualSaveFile)
            TICSaveEditor.Core.Save.SaveFileLoader.Load(File.ReadAllBytes(variantPath), variantPath);

        var baseSlot = MostRecentPopulatedSlot(baseline);
        var varSlot = MostRecentPopulatedSlot(variant);

        var baseUnit0 = baseline.Slots[baseSlot].SaveWork.Battle.Units[0];
        var varUnit0 = variant.Slots[varSlot].SaveWork.Battle.Units[0];

        Assert.Equal(0x09, baseUnit0.CombatSets[0].Skillset1);
        Assert.Equal(0x0E, varUnit0.CombatSets[0].Skillset1);
    }

    private static int MostRecentPopulatedSlot(TICSaveEditor.Core.Save.ManualSaveFile save)
    {
        int bestIdx = -1;
        DateTimeOffset bestTs = DateTimeOffset.MinValue;
        for (int i = 0; i < save.Slots.Count; i++)
        {
            if (save.Slots[i].IsEmpty) continue;
            var ts = save.Slots[i].SaveTimestamp;
            if (ts > bestTs) { bestTs = ts; bestIdx = i; }
        }
        return bestIdx;
    }

    [Fact]
    public void Mutating_typed_accessor_on_one_CombatSet_does_not_affect_others()
    {
        // CS1.MovementAbility writes to unit-relative 0x126 + 88 + 0x54 = 0x1D2.
        // Other presets must remain at their default (zero) values.
        var unit = new UnitSaveData(new byte[600]);
        unit.CombatSets[1].MovementAbility = 0xE6;

        Assert.Equal((ushort)0xE6, unit.CombatSets[1].MovementAbility);
        Assert.Equal((ushort)0,    unit.CombatSets[0].MovementAbility);
        Assert.Equal((ushort)0,    unit.CombatSets[2].MovementAbility);

        // Other typed accessors on the same preset must also stay at default.
        Assert.Equal((short)0, unit.CombatSets[1].Skillset0);
        Assert.Equal((short)0, unit.CombatSets[1].Skillset1);
        Assert.Equal((ushort)0, unit.CombatSets[1].ReactionAbility);
        Assert.Equal((ushort)0, unit.CombatSets[1].SupportAbility);
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

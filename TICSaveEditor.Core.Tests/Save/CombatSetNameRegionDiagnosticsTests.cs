using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;
using Xunit.Abstractions;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Diagnostic against <c>SaveFiles/{Baseline,CombatSetLongName}/enhanced.png</c>.
/// CombatSetLongName captures an in-game preset rename to exactly 16 ASCII chars
/// (the game UI's enforced maximum). Goal: discriminate between the two competing
/// 88-byte CombatSet name decompositions:
///   - spec/Nenkai: <c>Name[66]</c> (single ASCII null-terminated field)
///   - community:   <c>Name[16]</c> + <c>Padding[50]</c>
/// per <c>decisions_equipset_layout_resolved.md</c>.
///
/// Test passes trivially; xUnit ITestOutputHelper output is the deliverable.
/// </summary>
public class CombatSetNameRegionDiagnosticsTests
{
    private readonly ITestOutputHelper _out;

    public CombatSetNameRegionDiagnosticsTests(ITestOutputHelper output) => _out = output;

    private static string FixturePath(string folder) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "SaveFiles", folder, "enhanced.png");

    private static ManualSaveFile Load(string folder)
    {
        var path = FixturePath(folder);
        var bytes = File.ReadAllBytes(path);
        return (ManualSaveFile)SaveFileLoader.Load(bytes, path);
    }

    private static int MostRecentPopulatedSlot(ManualSaveFile save)
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

    private static byte[] UnitBytes(UnitSaveData unit)
    {
        var buf = new byte[600];
        unit.WriteTo(buf);
        return buf;
    }

    [Fact]
    public void T1_locate_renamed_combatset_and_dump_name_region()
    {
        var baseline = Load("Baseline");
        var variant = Load("CombatSetLongName");

        int baseSlot = MostRecentPopulatedSlot(baseline);
        int varSlot = MostRecentPopulatedSlot(variant);
        _out.WriteLine($"baseline experiment slot = {baseSlot}, variant experiment slot = {varSlot}");
        _out.WriteLine("");

        // CS regions inside UnitSaveData (264 bytes total at 0x126..0x22D, 88 bytes each).
        var csStarts = new[] { 0x126, 0x17E, 0x1D6 };
        var csNames = new[] { "CS0", "CS1", "CS2" };

        var baseBattle = baseline.Slots[baseSlot].SaveWork.Battle;
        var varBattle = variant.Slots[varSlot].SaveWork.Battle;

        // Sweep all 54 units to find the unit + CS whose name region (CS+0x00..0x41) differs.
        int foundUnit = -1, foundCs = -1;
        for (int u = 0; u < 54; u++)
        {
            var bUnit = baseBattle.Units[u];
            var vUnit = varBattle.Units[u];
            if (bUnit.IsEmpty && vUnit.IsEmpty) continue;

            var bBytes = UnitBytes(bUnit);
            var vBytes = UnitBytes(vUnit);

            for (int cs = 0; cs < 3; cs++)
            {
                int csStart = csStarts[cs];
                bool diffInName = false;
                for (int i = 0; i < 0x42; i++)
                {
                    if (bBytes[csStart + i] != vBytes[csStart + i]) { diffInName = true; break; }
                }
                if (diffInName)
                {
                    if (foundUnit < 0) { foundUnit = u; foundCs = cs; }
                    _out.WriteLine($"NAME-REGION DIFF: unit {u} {csNames[cs]} (char=0x{bUnit.Character:X2})");
                }
            }
        }

        if (foundUnit < 0)
        {
            _out.WriteLine("NO NAME-REGION DIFFS FOUND. Variant fixture may not have actually changed the name, or the change landed outside CS+0x00..0x41.");
            Assert.True(true, "Informational dump.");
            return;
        }

        // Dump full 88-byte CS region for the located preset, side-by-side, hi-lighting diffs.
        var bAll = UnitBytes(baseBattle.Units[foundUnit]);
        var vAll = UnitBytes(varBattle.Units[foundUnit]);
        int csOff = csStarts[foundCs];

        _out.WriteLine("");
        _out.WriteLine($"=== Renamed preset: unit {foundUnit} {csNames[foundCs]} (CS at unit-relative 0x{csOff:X3}) ===");
        _out.WriteLine("CS-rel  unit-rel  baseline  variant   diff?  ASCII-base  ASCII-var");
        _out.WriteLine("------  --------  --------  --------  -----  ----------  ---------");
        for (int i = 0; i < 0x42; i++)
        {
            byte bb = bAll[csOff + i];
            byte vb = vAll[csOff + i];
            string diffMark = bb != vb ? " *** " : "     ";
            char bChar = (bb >= 0x20 && bb < 0x7F) ? (char)bb : '.';
            char vChar = (vb >= 0x20 && vb < 0x7F) ? (char)vb : '.';
            _out.WriteLine($"  0x{i:X2}    0x{csOff + i:X3}     0x{bb:X2}      0x{vb:X2}    {diffMark}     {bChar}           {vChar}");
        }

        // Discriminating signals
        _out.WriteLine("");
        _out.WriteLine("=== Signals ===");

        // Signal 1: bytes 0x00..0x0F should hold the 16 ASCII chars in the variant.
        int ascii15Count = 0;
        for (int i = 0; i < 16; i++)
        {
            byte b = vAll[csOff + i];
            if (b >= 0x20 && b < 0x7F) ascii15Count++;
        }
        _out.WriteLine($"Signal 1 — ASCII printable count in variant bytes 0x00..0x0F: {ascii15Count}/16");
        if (ascii15Count == 16)
            _out.WriteLine("  -> Consistent with 16-char fixed-length name (community OR spec, both fit).");
        else if (ascii15Count == 15 && vAll[csOff + 15] == 0)
            _out.WriteLine("  -> 15-char string + null at byte 15: SPEC interpretation (game limit was effectively 15).");
        else
            _out.WriteLine("  -> Unexpected; contradicts both interpretations cleanly.");

        // Signal 2: byte 0x10 — the boundary byte.
        byte b16Base = bAll[csOff + 0x10];
        byte b16Var = vAll[csOff + 0x10];
        _out.WriteLine($"Signal 2 — byte 0x10 (boundary): baseline=0x{b16Base:X2} variant=0x{b16Var:X2}");
        if (b16Base == 0 && b16Var == 0)
            _out.WriteLine("  -> Both zero: INCONCLUSIVE (could be null terminator OR preserved padding zero).");
        else if (b16Base != 0 && b16Var == 0)
            _out.WriteLine("  -> Variant zeroed: SPEC (game wrote null terminator at byte 16 of a 66-byte name).");
        else if (b16Base != 0 && b16Var != 0 && b16Base == b16Var)
            _out.WriteLine("  -> Both nonzero AND equal: COMMUNITY (game preserved padding past byte 15).");
        else if (b16Base == 0 && b16Var != 0)
            _out.WriteLine("  -> Variant gained data at byte 16: COMMUNITY (16-byte name doesn't reach byte 16; game wrote into separate padding region).");
        else
            _out.WriteLine("  -> Both nonzero, values differ: NEEDS HUMAN — likely community but mutation pattern unclear.");

        // Signal 3: bytes 0x11..0x41 changes
        int rangeDiffs = 0;
        for (int i = 0x11; i < 0x42; i++)
            if (bAll[csOff + i] != vAll[csOff + i]) rangeDiffs++;
        _out.WriteLine($"Signal 3 — bytes 0x11..0x41 diff count: {rangeDiffs}/49");
        if (rangeDiffs == 0)
            _out.WriteLine("  -> Consistent with both interpretations.");
        else
            _out.WriteLine("  -> Unexpected: name change is mutating bytes deeper than 0x10. Surface for review.");

        Assert.True(true, "Informational dump.");
    }
}

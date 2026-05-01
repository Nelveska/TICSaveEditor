using TICSaveEditor.Core.Save;
using Xunit.Abstractions;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Diagnostic-only tests against the 2026-05-01 fixture set under
/// <c>SaveFiles/{Baseline,ChangeOneItem,ChangeOneAbilitySlot,ChangeOneSkillset}/enhanced.png</c>.
/// Goal: identify which bytes changed in each variant relative to Baseline so we can
/// (a) confirm the fixtures isolate the intended single-edit change, and
/// (b) discriminate between the spec/Nenkai vs community CombatSet decompositions
///     (or, if the edits are to inline equipment, identify the inline slot affected).
///
/// Tests pass trivially; xUnit ITestOutputHelper output is the deliverable.
/// Plan: C:/Users/SHODAN/.claude/plans/what-s-next-up-peppy-flamingo.md (Task 3).
/// </summary>
public class CombatSetFixtureDiagnosticsTests
{
    private readonly ITestOutputHelper _out;

    public CombatSetFixtureDiagnosticsTests(ITestOutputHelper output) => _out = output;

    private static string FixturePath(string folder) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "SaveFiles", folder, "enhanced.png");

    private static ManualSaveFile Load(string folder)
    {
        var path = FixturePath(folder);
        var bytes = File.ReadAllBytes(path);
        return (ManualSaveFile)SaveFileLoader.Load(bytes, path);
    }

    private static int[] PopulatedSlotIndices(ManualSaveFile save)
    {
        var result = new List<int>();
        for (int i = 0; i < save.Slots.Count; i++)
            if (!save.Slots[i].IsEmpty) result.Add(i);
        return result.ToArray();
    }

    /// <summary>Most recently saved populated slot identifies the experiment slot.</summary>
    private static int MostRecentSlotIndex(ManualSaveFile save)
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

    private static string OffsetLabel(int unitOffset) => unitOffset switch
    {
        < 0x08 => "identity",
        < 0x0E => "abilities-inline",
        < 0x1C => "equip-inline",
        < 0x20 => "exp/lvl/brv/fth",
        < 0x32 => "stat-base",
        < 0x74 => "ability_flag",
        < 0x80 => "job_level",
        < 0xAE => "job_point",
        < 0xDC => "total_jp",
        < 0x11C => "name-region",
        < 0x126 => "slot-meta",
        < 0x180 => "CombatSet0",
        < 0x1DA => "CombatSet1",
        < 0x22E => "CombatSet2",
        _ => "trailing"
    };

    [Fact]
    public void T1_inventory_each_fixture_basic_state()
    {
        foreach (var folder in new[] { "Baseline", "ChangeOneItem", "ChangeOneAbilitySlot", "ChangeOneSkillset" })
        {
            var save = Load(folder);
            var populated = PopulatedSlotIndices(save);
            _out.WriteLine($"### {folder}: {populated.Length} populated slots, indices [{string.Join(", ", populated)}]");
            foreach (var i in populated)
            {
                var s = save.Slots[i];
                var title = (s.SlotTitle ?? string.Empty).Trim();
                if (title.Length > 32) title = title[..32];
                _out.WriteLine($"  slot {i,2}: {title,-32}  ts={s.SaveTimestamp:O}  pt={s.Playtime}");
            }
            _out.WriteLine($"  most recent slot: {MostRecentSlotIndex(save)}");
            _out.WriteLine("");
        }
        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T2_diff_baseline_vs_each_variant_section_level()
    {
        var baseline = Load("Baseline");
        var baselineSlot = MostRecentSlotIndex(baseline);
        var baselineBytes = baseline.Slots[baselineSlot].SaveWork.RawBytes;
        _out.WriteLine($"Baseline experiment slot: {baselineSlot}");
        _out.WriteLine("");

        foreach (var folder in new[] { "ChangeOneItem", "ChangeOneAbilitySlot", "ChangeOneSkillset" })
        {
            var variant = Load(folder);
            var variantSlot = MostRecentSlotIndex(variant);
            var variantBytes = variant.Slots[variantSlot].SaveWork.RawBytes;

            int totalDiffs = 0;
            for (int i = 0; i < baselineBytes.Length; i++)
                if (baselineBytes[i] != variantBytes[i]) totalDiffs++;

            _out.WriteLine($"### {folder}: experiment slot = {variantSlot}, total bytes differ vs baseline slot {baselineSlot} = {totalDiffs}");

            // Quick bucket by SaveWork section to show where changes cluster.
            var sectionMap = new (string Name, int Offset, int Size)[]
            {
                ("Card",            0x0000, 0x0100),
                ("Info",            0x0100, 0x00B8),
                ("World",           0x01B8, 0x0360),
                ("Battle",          0x0518, 0x8F48),
                ("User",            0x9460, 0x0064),
                ("FftoWorld",       0x94C4, 0x0208),
                ("FftoBattle",      0x96CC, 0x00C2),
                ("FftoAchievement", 0x978E, 0x00AC),
                ("FftoConfig",      0x983A, 0x0001),
                ("FftoBraveStory",  0x983B, 0x0495),
                ("TrailingUnk",     0x9CD0, 0x000C),
            };
            foreach (var (name, off, size) in sectionMap)
            {
                int count = 0;
                for (int i = 0; i < size; i++)
                    if (baselineBytes[off + i] != variantBytes[off + i]) count++;
                if (count > 0)
                    _out.WriteLine($"  {name,-16} +{off:X4} size={size:X4}  bytes-diff={count}");
            }
            _out.WriteLine("");
        }
        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T3_diff_per_unit_within_battle_section()
    {
        // BattleSection contains 54 × UnitSaveData (600 bytes each) at offsets 0..0x7E90,
        // then trailing inventory + EventWork. We diff each unit slot to localize which
        // unit was edited and which byte ranges within UnitSaveData differ. The byte
        // range within UnitSaveData tells us which sub-region was edited:
        //   0x0E-0x1B: inline equipment (7 × u16)
        //   0x126-0x17D: CombatSet0 (Name 66 + ItemBytes 10 + AbilityBytes 10 + Job 1 + DH 1 = 88)
        //   0x17E-0x1D5: CombatSet1
        //   0x1D6-0x22D: CombatSet2
        // If the change is in a CombatSet block at offset >= 0x42 within the block, the
        // exact byte offsets discriminate Items[0x42-0x4B] vs Abilities[0x4C-0x55] vs the
        // community alternative (Items 0x42-0x4B + Skillsets 0x4C-0x4F + Abilities 0x50-0x55).
        var baseline = Load("Baseline");
        var baselineSlot = MostRecentSlotIndex(baseline);
        var baselineBattle = baseline.Slots[baselineSlot].SaveWork.Battle;

        foreach (var folder in new[] { "ChangeOneItem", "ChangeOneAbilitySlot", "ChangeOneSkillset" })
        {
            var variant = Load(folder);
            var variantSlot = MostRecentSlotIndex(variant);
            var variantBattle = variant.Slots[variantSlot].SaveWork.Battle;

            _out.WriteLine($"### {folder}: per-unit byte diffs (baseline slot {baselineSlot} -> variant slot {variantSlot})");
            for (int u = 0; u < 54; u++)
            {
                var baseUnit = baselineBattle.Units[u];
                var varUnit = variantBattle.Units[u];
                if (baseUnit.IsEmpty && varUnit.IsEmpty) continue;

                var baseBytes = new byte[600];
                var varBytes = new byte[600];
                baseUnit.WriteTo(baseBytes);
                varUnit.WriteTo(varBytes);

                var diffs = new List<int>();
                for (int i = 0; i < 600; i++)
                    if (baseBytes[i] != varBytes[i]) diffs.Add(i);

                if (diffs.Count == 0) continue;

                _out.WriteLine($"  unit {u,2} (char=0x{baseUnit.Character:X2}): {diffs.Count} byte(s) differ");
                int shown = 0;
                foreach (var off in diffs)
                {
                    if (shown++ > 32)
                    {
                        _out.WriteLine($"    ... ({diffs.Count - 32} more diffs suppressed)");
                        break;
                    }
                    int withinCs = -1;
                    string? csNote = null;
                    if (off >= 0x126 && off < 0x17E) { withinCs = off - 0x126; csNote = "CS0"; }
                    else if (off >= 0x17E && off < 0x1D6) { withinCs = off - 0x17E; csNote = "CS1"; }
                    else if (off >= 0x1D6 && off < 0x22E) { withinCs = off - 0x1D6; csNote = "CS2"; }

                    var section = OffsetLabel(off);
                    var csStr = csNote != null ? $"  [{csNote}+0x{withinCs:X2}]" : string.Empty;
                    _out.WriteLine($"    0x{off:X3} ({section}): base=0x{baseBytes[off]:X2} variant=0x{varBytes[off]:X2}{csStr}");
                }
            }
            _out.WriteLine("");
        }
        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T4_check_inline_equipment_slots()
    {
        // If the user's "ChangeOneItem" was edited via the inline equipment editor (not a
        // CombatSet preset), the diff lives at UnitSaveData 0x0E-0x1B (7 × u16). This test
        // dumps inline equipment for the experiment unit across all 4 fixtures so we can
        // see in one glance whether inline differs.
        var folders = new[] { "Baseline", "ChangeOneItem", "ChangeOneAbilitySlot", "ChangeOneSkillset" };
        var saves = folders.ToDictionary(f => f, Load);
        var slots = saves.ToDictionary(kv => kv.Key, kv => MostRecentSlotIndex(kv.Value));

        _out.WriteLine("Inline equipment (7 × u16 at UnitSaveData 0x0E-0x1B) across fixtures:");
        _out.WriteLine("Showing units 0..6 from each fixture's experiment slot.");
        _out.WriteLine("");
        for (int u = 0; u <= 6; u++)
        {
            _out.WriteLine($"=== unit {u} ===");
            foreach (var folder in folders)
            {
                var unit = saves[folder].Slots[slots[folder]].SaveWork.Battle.Units[u];
                if (unit.IsEmpty) { _out.WriteLine($"  {folder,-22} EMPTY"); continue; }
                var bytes = new byte[600];
                unit.WriteTo(bytes);
                var equipSlots = new ushort[7];
                for (int s = 0; s < 7; s++)
                    equipSlots[s] = (ushort)(bytes[0x0E + s * 2] | (bytes[0x0F + s * 2] << 8));
                var hex = string.Join(" ", equipSlots.Select(v => v.ToString("X4")));
                _out.WriteLine($"  {folder,-22} char=0x{unit.Character:X2}  inline-equip = [{hex}]");
            }
            _out.WriteLine("");
        }
        Assert.True(true, "Informational dump.");
    }
}

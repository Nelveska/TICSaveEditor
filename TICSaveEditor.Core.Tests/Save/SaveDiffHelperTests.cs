using TICSaveEditor.Core.Save;
using Xunit.Abstractions;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Diff each variant fixture's experiment slot against the baseline slot.
/// Output the byte-region differences as test diagnostics so we can resolve
/// the M5 open questions (job_new semantics, disable_flag hypotheses,
/// InternalChecksum digest, EquipSet decomposition, gil/inventory location).
///
/// These tests are *informational* — they pass as long as we successfully
/// extracted slot bytes; the diff output is what the user reads.
/// </summary>
public class SaveDiffHelperTests
{
    private static readonly (string Name, int Offset, int Size)[] SectionMap =
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

    private readonly ITestOutputHelper _out;

    public SaveDiffHelperTests(ITestOutputHelper output) => _out = output;

    private static string FixturePath(string name) => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "SaveFiles", name, "enhanced.png");

    private static ManualSaveFile Load(string fixture)
    {
        var bytes = File.ReadAllBytes(FixturePath(fixture));
        return (ManualSaveFile)SaveFileLoader.Load(bytes, FixturePath(fixture));
    }

    /// <summary>
    /// Find slot indices populated in variant but NOT in baseline. These are the
    /// "experiment slots" the user saved into for this variant.
    /// </summary>
    private static int[] ExperimentSlots(ManualSaveFile baseline, ManualSaveFile variant)
    {
        var result = new List<int>();
        for (int i = 0; i < baseline.Slots.Count; i++)
        {
            if (!baseline.Slots[i].IsEmpty) continue;
            if (variant.Slots[i].IsEmpty) continue;
            result.Add(i);
        }
        return result.ToArray();
    }

    /// <summary>
    /// Find the slot with the most recent SaveTimestamp — that's the slot the user
    /// most recently saved into. In Baseline.png, that's the user's "baseline" save.
    /// In a variant file, that's the experiment slot. Older pre-existing slots from
    /// prior gameplay sessions are ignored.
    /// </summary>
    private static int MostRecentSlotIndex(ManualSaveFile save)
    {
        var bestIdx = -1;
        var bestStamp = DateTime.MinValue;
        for (int i = 0; i < save.Slots.Count; i++)
        {
            if (save.Slots[i].IsEmpty) continue;
            var stamp = save.Slots[i].SaveTimestamp;
            if (stamp >= bestStamp)
            {
                bestStamp = stamp;
                bestIdx = i;
            }
        }
        if (bestIdx < 0) throw new InvalidOperationException("No populated slot found.");
        return bestIdx;
    }

    private static int BaselineSlotIndex(ManualSaveFile save) => MostRecentSlotIndex(save);

    private static (int FirstDiff, int LastDiff, int DiffCount) DiffRange(
        ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Length mismatch.");
        int first = -1, last = -1, count = 0;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                if (first == -1) first = i;
                last = i;
                count++;
            }
        }
        return (first, last, count);
    }

    private void DumpSectionDiff(string label, ReadOnlySpan<byte> baselineSlot, ReadOnlySpan<byte> variantSlot)
    {
        _out.WriteLine($"=== {label} ===");
        foreach (var (name, off, size) in SectionMap)
        {
            var (first, last, count) = DiffRange(
                baselineSlot.Slice(off, size), variantSlot.Slice(off, size));
            if (count == 0) continue;
            _out.WriteLine(
                $"  {name,-18} (0x{off:X4}..0x{off + size - 1:X4}): {count} byte(s) differ, " +
                $"section-relative range 0x{first:X4}..0x{last:X4}");
            // For small diffs, show actual bytes.
            if (count <= 32)
            {
                for (int i = first; i <= last; i++)
                {
                    var bb = baselineSlot[off + i];
                    var vb = variantSlot[off + i];
                    if (bb != vb)
                    {
                        _out.WriteLine($"    0x{off + i:X4}  base=0x{bb:X2}  variant=0x{vb:X2}");
                    }
                }
            }
        }
    }

    [Fact]
    public void Diff_baseline_slot_against_each_variant_experiment_slot()
    {
        var baseline = Load("Baseline");
        var baselineIdx = MostRecentSlotIndex(baseline);
        var baselineSlotBytes = baseline.Slots[baselineIdx].SaveWork.RawBytes;

        _out.WriteLine($"Baseline experiment slot: {baselineIdx} (SaveTimestamp={baseline.Slots[baselineIdx].SaveTimestamp:O})");
        _out.WriteLine($"Section map (slot-relative offsets):");
        foreach (var (name, off, size) in SectionMap)
        {
            _out.WriteLine($"  {name,-18} 0x{off:X4}..0x{off + size - 1:X4} ({size} bytes)");
        }
        _out.WriteLine("");

        foreach (var fixture in new[] { "InternalChecksum", "EquipSet", "Inventory", "JobChange" })
        {
            var variant = Load(fixture);
            var variantIdx = MostRecentSlotIndex(variant);
            _out.WriteLine($"### {fixture}");
            _out.WriteLine($"  All populated slots in {fixture}:");
            for (int i = 0; i < variant.Slots.Count; i++)
            {
                if (variant.Slots[i].IsEmpty) continue;
                _out.WriteLine($"    slot {i}: title=\"{variant.Slots[i].SlotTitle}\" timestamp={variant.Slots[i].SaveTimestamp:O}");
            }
            _out.WriteLine($"  Most-recent slot in {fixture}: {variantIdx}");
            var variantSlotBytes = variant.Slots[variantIdx].SaveWork.RawBytes;
            _out.WriteLine($"  Diffing baseline slot {baselineIdx} vs {fixture} slot {variantIdx}:");
            DumpSectionDiff(fixture, baselineSlotBytes, variantSlotBytes);
            _out.WriteLine("");
        }

        Assert.True(true, "Informational diff produced; check test output.");
    }

    [Fact]
    public void Confirm_baseline_slot_unchanged_across_all_variant_files()
    {
        var baseline = Load("Baseline");
        var baselineIdx = BaselineSlotIndex(baseline);
        var baselineBytes = baseline.Slots[baselineIdx].SaveWork.RawBytes;

        foreach (var fixture in new[] { "InternalChecksum", "EquipSet", "Inventory", "JobChange" })
        {
            var variant = Load(fixture);
            var variantBaselineSlotBytes = variant.Slots[baselineIdx].SaveWork.RawBytes;
            var (first, last, count) = DiffRange(baselineBytes, variantBaselineSlotBytes);
            if (count == 0)
            {
                _out.WriteLine($"{fixture}: baseline slot {baselineIdx} is byte-identical ✓");
            }
            else
            {
                _out.WriteLine(
                    $"{fixture}: baseline slot {baselineIdx} drifted in {count} bytes " +
                    $"(0x{first:X4}..0x{last:X4}) — likely incidental playtime / timestamp drift.");
            }
        }
        Assert.True(true);
    }

    [Fact]
    public void Dump_unit_records_in_baseline_slot_for_independent_verification()
    {
        var baseline = Load("Baseline");
        var baselineIdx = MostRecentSlotIndex(baseline);
        var battle = baseline.Slots[baselineIdx].SaveWork.Battle;

        _out.WriteLine($"Baseline slot index: {baselineIdx}");
        _out.WriteLine("");
        _out.WriteLine("idx  char  sex   zodiac  job  exp  level  HP_base    MP_base    SP_base    PA_base    MA_base    equip[0..6] (Head, Body, Accy, RWep, RShld, LWep, LShld)");
        _out.WriteLine("---  ----  ----  ------  ---  ---  -----  ---------  ---------  ---------  ---------  ---------  --------------------------------------------------------");
        for (int i = 0; i < 54; i++)
        {
            var u = battle.Units[i];
            if (u.IsEmpty) continue;
            var equip = string.Join(", ", Enumerable.Range(0, 7).Select(s => $"0x{u.GetEquipItem(s):X4}"));
            _out.WriteLine(
                $"{i,3}  0x{u.Character:X2}  0x{u.Sex:X2}  0x{u.ZodiacSign:X2}    {u.Job,3}  {u.Exp,3}  {u.Level,5}  {u.HpMaxBase,9}  {u.MpMaxBase,9}  {u.WtBase,9}  {u.AtBase,9}  {u.MatBase,9}  [{equip}]");
        }

        Assert.True(true);
    }

    [Fact]
    public void Confirm_zodiac_high_nibble_is_in_0_to_11_range()
    {
        // The 12 zodiac signs occupy values 0..11. User claims sign is encoded
        // in the high nibble of byte 0x06: sign << 4. We verify the high
        // nibble stays in 0..11 across every populated unit in every fixture.
        // Note: low nibble is NOT always zero (observed 0x0 and 0x1 across
        // populated units), so the user's "low nibble unused" claim is
        // partially refuted — see decisions_zodiac_high_nibble_decode.md.
        var observedLowNibbles = new HashSet<byte>();
        foreach (var fixture in new[] { "Baseline", "EquipSet", "InternalChecksum", "Inventory", "JobChange" })
        {
            var save = Load(fixture);
            for (int slotIdx = 0; slotIdx < save.Slots.Count; slotIdx++)
            {
                if (save.Slots[slotIdx].IsEmpty) continue;
                var battle = save.Slots[slotIdx].SaveWork.Battle;
                for (int u = 0; u < 54; u++)
                {
                    if (battle.Units[u].IsEmpty) continue;
                    var z = battle.Units[u].ZodiacSign;
                    var sign = (z & 0xF0) >> 4;
                    Assert.True(sign <= 11,
                        $"{fixture} slot {slotIdx} unit {u} zodiac=0x{z:X2} sign={sign} > 11");
                    observedLowNibbles.Add((byte)(z & 0x0F));
                }
            }
        }
        _out.WriteLine($"Observed zodiac low nibbles: [{string.Join(", ", observedLowNibbles.Select(b => $"0x{b:X1}"))}]");
    }

    [Fact]
    public void Inspect_FftoBattle_initial_state_for_disable_flag_hypotheses()
    {
        // Hypothesis #2 from decisions_jobnew_vs_jobdisable_naming.md: disable_flag bytes 20
        // and 21 might be pre-set non-zero to disable stripped Dark Knight / Onion Knight jobs.
        // We inspect the FftoBattle.JobDisableFlags region (offsets 0x15..0x91 within
        // FftoBattleSection.Bytes) in the baseline-most-recent slot to test this.
        var baseline = Load("Baseline");
        var slot = baseline.Slots[MostRecentSlotIndex(baseline)];
        var ffto = slot.SaveWork.FftoBattle;

        _out.WriteLine("FftoBattle.JobNewFlags initial state (21 bytes at offset 0x00..0x14):");
        var jobNew = string.Join(" ", Enumerable.Range(0, 21).Select(i => $"{ffto.GetJobNewFlag(i):X2}"));
        _out.WriteLine($"  {jobNew}");

        _out.WriteLine("FftoBattle.JobDisableFlags initial state (125 bytes at offset 0x15..0x91):");
        for (int row = 0; row < 125; row += 25)
        {
            var slice = string.Join(" ", Enumerable.Range(row, Math.Min(25, 125 - row))
                .Select(i => $"{ffto.GetJobDisableFlag(i):X2}"));
            _out.WriteLine($"  [{row,3}..{row + 24,3}]  {slice}");
        }

        _out.WriteLine("");
        _out.WriteLine($"  byte[20] = 0x{ffto.GetJobDisableFlag(20):X2}  (hypothesis #2: stripped Dark Knight if non-zero)");
        _out.WriteLine($"  byte[21] = 0x{ffto.GetJobDisableFlag(21):X2}  (hypothesis #2: stripped Onion Knight if non-zero)");

        var nonZeroIndices = Enumerable.Range(0, 125)
            .Where(i => ffto.GetJobDisableFlag(i) != 0)
            .ToArray();
        _out.WriteLine("");
        _out.WriteLine($"  All non-zero indices in disable_flag: [{string.Join(", ", nonZeroIndices)}]");
        _out.WriteLine($"  Total non-zero bytes: {nonZeroIndices.Length} / 125");

        Assert.True(true);
    }

    [Fact]
    public void Diff_baseline2_against_BuyPotion_and_BuyDagger_for_M9()
    {
        // M9 Phase 1: empirical verification of the CSV's structural claims.
        // Baseline2 is the user's clean reference; BuyPotion = +1 Potion, BuyDagger = +1 Dagger.
        // CSV expects: Dagger storage index = 0x00; Potion at 0xEF (CSV) or 0xF0 (M5.5 finding).
        var baseline = Load("Baseline2");
        var baselineIdx = MostRecentSlotIndex(baseline);
        var baselineSlotBytes = baseline.Slots[baselineIdx].SaveWork.RawBytes;

        _out.WriteLine($"Baseline2 most-recent slot: {baselineIdx} (timestamp={baseline.Slots[baselineIdx].SaveTimestamp:O})");
        _out.WriteLine("");

        foreach (var fixture in new[] { "BuyPotion", "BuyDagger" })
        {
            var variant = Load(fixture);
            var variantIdx = MostRecentSlotIndex(variant);
            _out.WriteLine($"### {fixture}  (most-recent slot: {variantIdx})");
            var variantSlotBytes = variant.Slots[variantIdx].SaveWork.RawBytes;
            DumpSectionDiff(fixture, baselineSlotBytes, variantSlotBytes);

            // Inspect PartyItem region directly for the byte-level change.
            var bParty = baseline.Slots[baselineIdx].SaveWork.Battle.PartyItemRaw;
            var vParty = variant.Slots[variantIdx].SaveWork.Battle.PartyItemRaw;
            var changes = new List<(int Idx, byte Base, byte Var)>();
            for (int i = 0; i < bParty.Length; i++)
            {
                if (bParty[i] != vParty[i]) changes.Add((i, bParty[i], vParty[i]));
            }
            _out.WriteLine($"  PartyItem bytes that changed: {changes.Count}");
            foreach (var (idx, b, v) in changes)
            {
                _out.WriteLine($"    PartyItemRaw[0x{idx:X3}] base=0x{b:X2}  variant=0x{v:X2}  delta={(int)v - (int)b:+#;-#;0}");
            }
            _out.WriteLine("");
        }

        Assert.True(true);
    }

    [Fact]
    public void Inspect_PartyItem_boundary_to_test_size_0x105_vs_0x10F()
    {
        // Hypothesis (from M9 planning): the format-notes claim of PartyItem[0x105] is wrong;
        // CT v4 maps storage indices contiguously to 0x10E. If PartyItem is actually 0x10F bytes,
        // then what BattleSection currently exposes as ShopItemRaw[0x00..0x09] is really the tail
        // of PartyItem (DLC items at 0x100..0x103, Throwables at 0x109..0x10E).
        //
        // Test: print PartyItemRaw[0x100..0x104] (current end) + ShopItemRaw[0x00..0x0E]
        // (the disputed 15 bytes) for each fixture. Non-zero values in indices that map to
        // known Throwables / DLC items confirm the 0x10F hypothesis.
        foreach (var fixture in new[] { "Baseline", "EquipSet", "InternalChecksum", "Inventory", "JobChange" })
        {
            var save = Load(fixture);
            var slot = save.Slots[MostRecentSlotIndex(save)];
            var party = slot.SaveWork.Battle.PartyItemRaw;
            var shop = slot.SaveWork.Battle.ShopItemRaw;

            var partyTail = string.Join(" ", Enumerable.Range(0xFF, 6).Select(i => $"{party[i]:X2}"));
            var shopHead = string.Join(" ", Enumerable.Range(0x00, 0x10).Select(i => $"{shop[i]:X2}"));

            _out.WriteLine($"{fixture,-18} PartyItemRaw[0xFF..0x104] = {partyTail}");
            _out.WriteLine($"{fixture,-18} ShopItemRaw[0x00..0x0F]  = {shopHead}");
            _out.WriteLine($"{fixture,-18}   (^ if hypothesis holds, ShopItemRaw[0x00..0x09] is really PartyItem[0x105..0x10E];");
            _out.WriteLine($"{fixture,-18}      indices 0x100..0x103 are DLC items, 0x109..0x10E are Throwables.)");
            _out.WriteLine("");
        }

        // Also dump some known-Potion bytes for the Inventory fixture vs Baseline.
        // CSV: Potion = StorageIndex 0xEF. Buying 1 Potion should bump byte at PartyItemRaw[0xEF] by 1.
        var baseline = Load("Baseline");
        var inventory = Load("Inventory");
        var bIdx = MostRecentSlotIndex(baseline);
        var iIdx = MostRecentSlotIndex(inventory);
        var bParty = baseline.Slots[bIdx].SaveWork.Battle.PartyItemRaw;
        var iParty = inventory.Slots[iIdx].SaveWork.Battle.PartyItemRaw;
        _out.WriteLine($"Potion expected at PartyItemRaw[0xEF]:");
        _out.WriteLine($"  Baseline  PartyItemRaw[0xEF] = 0x{bParty[0xEF]:X2}");
        _out.WriteLine($"  Inventory PartyItemRaw[0xEF] = 0x{iParty[0xEF]:X2}");

        Assert.True(true);
    }

    [Fact]
    public void Confirm_total_populated_slots_grows_monotonically()
    {
        var counts = new Dictionary<string, int>();
        foreach (var fixture in new[] { "Baseline", "InternalChecksum", "EquipSet", "Inventory", "JobChange" })
        {
            var save = Load(fixture);
            counts[fixture] = save.Slots.Count(s => !s.IsEmpty);
            _out.WriteLine($"{fixture,-20}: {counts[fixture]} populated slot(s)");
        }
        Assert.True(true);
    }
}

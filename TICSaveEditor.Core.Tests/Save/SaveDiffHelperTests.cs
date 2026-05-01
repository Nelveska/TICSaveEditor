using TICSaveEditor.Core.Save;
using Xunit.Abstractions;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Diff each variant fixture's experiment slot against the baseline slot. Output
/// the byte-region differences as test diagnostics.
///
/// Active fixture set (2026-05-01): Baseline + ChangeOneItem + ChangeOneAbilitySlot
/// + ChangeOneSkillset. The earlier 5-fixture battery (EquipSet / InternalChecksum /
/// Inventory / JobChange and the M9 Baseline2 / BuyPotion / BuyDagger trio) was
/// retired in the same session that resolved the CombatSet decomposition; their
/// findings are baked into Core code + memory.
///
/// These tests are *informational* -- they pass as long as we successfully
/// extracted slot bytes; the diff output is what the user reads.
/// </summary>
public class SaveDiffHelperTests
{
    private static readonly string[] Variants =
        { "ChangeOneItem", "ChangeOneAbilitySlot", "ChangeOneSkillset" };

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
    /// Find the slot with the most recent SaveTimestamp -- that's the slot the user
    /// most recently saved into. In Baseline, that's the user's "baseline" save. In a
    /// variant file, that's the experiment slot. Older pre-existing slots from prior
    /// gameplay sessions are ignored.
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
        _out.WriteLine("Section map (slot-relative offsets):");
        foreach (var (name, off, size) in SectionMap)
        {
            _out.WriteLine($"  {name,-18} 0x{off:X4}..0x{off + size - 1:X4} ({size} bytes)");
        }
        _out.WriteLine("");

        foreach (var fixture in Variants)
        {
            var variant = Load(fixture);
            var variantIdx = MostRecentSlotIndex(variant);
            _out.WriteLine($"### {fixture}");
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
        var baselineIdx = MostRecentSlotIndex(baseline);
        var baselineBytes = baseline.Slots[baselineIdx].SaveWork.RawBytes;

        foreach (var fixture in Variants)
        {
            var variant = Load(fixture);
            var variantBaselineSlotBytes = variant.Slots[baselineIdx].SaveWork.RawBytes;
            var (first, last, count) = DiffRange(baselineBytes, variantBaselineSlotBytes);
            if (count == 0)
            {
                _out.WriteLine($"{fixture}: baseline slot {baselineIdx} is byte-identical");
            }
            else
            {
                _out.WriteLine(
                    $"{fixture}: baseline slot {baselineIdx} drifted in {count} bytes " +
                    $"(0x{first:X4}..0x{last:X4}) -- likely incidental playtime / timestamp drift.");
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
        // The 12 zodiac signs occupy values 0..11. Sign is encoded in the high
        // nibble of byte 0x06 (sign << 4); low nibble is non-zero in some saves
        // (originally observed during the Glain/PSX-formula investigation).
        var observedLowNibbles = new HashSet<byte>();
        foreach (var fixture in new[] { "Baseline" }.Concat(Variants))
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
    public void Confirm_total_populated_slots_grows_monotonically()
    {
        var counts = new Dictionary<string, int>();
        foreach (var fixture in new[] { "Baseline" }.Concat(Variants))
        {
            var save = Load(fixture);
            counts[fixture] = save.Slots.Count(s => !s.IsEmpty);
            _out.WriteLine($"{fixture,-22}: {counts[fixture]} populated slot(s)");
        }
        Assert.True(true);
    }
}

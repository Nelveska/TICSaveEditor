using System.Text;
using TICSaveEditor.Core.Save;
using Xunit.Abstractions;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Diagnostic-only tests against the user's <c>SaveFiles/enhanced.png</c> playthrough
/// fixture (10 sequential manual saves spanning a chapter-1 stretch). They exist to
/// resolve two anomalies the synthetic 5-fixture battery missed:
///
///   (A) Generic units renamed in-game render under their pre-rename names. The
///       Docs/fft-save-format-notes.md "world saves zero-fill chr_name" claim
///       (line 336) was inferred from baseline fixtures with no rename history.
///   (B) Argath persists in unit slot 51 after a story-scripted departure. The
///       guest-active/departed encoding is currently OPEN in the format docs
///       (line 445).
///
/// Tests pass trivially; the xUnit ITestOutputHelper dump is what we read.
/// Plan: C:/Users/SHODAN/.claude/plans/i-have-found-something-delightful-candy.md
/// </summary>
public class EnhancedFixtureDiagnosticsTests
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

    public EnhancedFixtureDiagnosticsTests(ITestOutputHelper output) => _out = output;

    private static string FixturePath() => Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "SaveFiles", "enhanced.png");

    private static ManualSaveFile Load()
    {
        var path = FixturePath();
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

    private static string AsciiPrintable(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
            sb.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
        return sb.ToString();
    }

    private static string Hex(ReadOnlySpan<byte> bytes, int bytesPerGroup = 4)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        for (int i = 0; i < bytes.Length; i++)
        {
            sb.Append(bytes[i].ToString("X2"));
            sb.Append(((i + 1) % bytesPerGroup) == 0 ? "  " : " ");
        }
        return sb.ToString().TrimEnd();
    }

    [Fact]
    public void T1_chronology_and_slot_metadata()
    {
        var save = Load();
        var populated = PopulatedSlotIndices(save);
        _out.WriteLine($"enhanced.png  populated slot indices: [{string.Join(", ", populated)}]");
        _out.WriteLine($"  total populated: {populated.Length} / {save.Slots.Count}");
        _out.WriteLine("");
        _out.WriteLine("idx  title                                 timestamp                    playtime");
        _out.WriteLine("---  ------------------------------------  ---------------------------  -------------");
        foreach (var i in populated)
        {
            var s = save.Slots[i];
            var title = (s.SlotTitle ?? string.Empty).Trim();
            if (title.Length > 36) title = title[..36];
            _out.WriteLine($"{i,3}  {title,-36}  {s.SaveTimestamp:O}  {s.Playtime}");
        }

        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T2_rename_storage_for_units_0_through_6()
    {
        // For every populated slot, dump for unit slots 0..6:
        //   Character / Sex / NameNo / CharaNameKey
        //   UnitNicknameRaw (16 bytes) hex + ASCII column + CP932 attempt
        // Primary hypothesis: UnitNicknameRaw transitions from zero to a renamed
        // string at the rename boundary. NameNo / CharaNameKey unchanged across
        // the boundary.
        var save = Load();
        var populated = PopulatedSlotIndices(save);

        Encoding? cp932 = null;
        try { cp932 = Encoding.GetEncoding(932); }
        catch { /* fallback path: no CP932 available; ASCII column still informative */ }

        foreach (var slotIdx in populated)
        {
            var battle = save.Slots[slotIdx].SaveWork.Battle;
            _out.WriteLine($"### slot {slotIdx}  (timestamp={save.Slots[slotIdx].SaveTimestamp:O})");
            for (int u = 0; u <= 6; u++)
            {
                var unit = battle.Units[u];
                if (unit.IsEmpty)
                {
                    _out.WriteLine($"  unit {u,2}: EMPTY");
                    continue;
                }
                var raw = unit.UnitNicknameRaw;
                var nullIdx = Array.IndexOf(raw, (byte)0);
                var len = nullIdx < 0 ? raw.Length : nullIdx;
                var asciiPart = AsciiPrintable(raw.AsSpan(0, len));
                var cp932Part = (cp932 != null && len > 0)
                    ? cp932.GetString(raw, 0, len)
                    : "(cp932 N/A)";
                var firstByteIsZero = raw[0] == 0;
                var anyNonZero = !firstByteIsZero || raw.Any(b => b != 0);

                _out.WriteLine(
                    $"  unit {u,2}: char=0x{unit.Character:X2}  sex=0x{unit.Sex:X2}  job={unit.Job,3}  " +
                    $"NameNo={unit.NameNo,5}  CharaNameKey={unit.CharaNameKey,5}  " +
                    $"UnitNicknameRaw any-nonzero={anyNonZero}  null-idx={(nullIdx < 0 ? "(none)" : nullIdx.ToString())}");
                if (anyNonZero)
                {
                    // Show first 32 bytes (covers any reasonable rename string).
                    _out.WriteLine($"        bytes[0..31] = {Hex(raw.AsSpan(0, Math.Min(32, raw.Length)))}");
                    _out.WriteLine($"        ASCII        = \"{asciiPart}\"");
                    _out.WriteLine($"        CP932        = \"{cp932Part}\"");
                }
            }
            _out.WriteLine("");
        }

        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T2b_full_unit_byte_diff_across_rename_boundary()
    {
        // Fallback if T2's primary hypothesis fails: byte-level diff full UnitSaveData
        // (600 bytes) for unit slots 1..6 across consecutive populated slots. The
        // rename has to be stored *somewhere*; this finds where.
        var save = Load();
        var populated = PopulatedSlotIndices(save);

        for (int p = 0; p < populated.Length - 1; p++)
        {
            var aIdx = populated[p];
            var bIdx = populated[p + 1];
            var aBattle = save.Slots[aIdx].SaveWork.Battle;
            var bBattle = save.Slots[bIdx].SaveWork.Battle;

            _out.WriteLine($"### diff: slot {aIdx} → slot {bIdx}");
            for (int u = 1; u <= 6; u++)
            {
                var aUnit = aBattle.Units[u];
                var bUnit = bBattle.Units[u];
                if (aUnit.IsEmpty && bUnit.IsEmpty) continue;

                var aBytes = new byte[600];
                var bBytes = new byte[600];
                aUnit.WriteTo(aBytes);
                bUnit.WriteTo(bBytes);

                var diffs = new List<int>();
                for (int i = 0; i < 600; i++)
                    if (aBytes[i] != bBytes[i]) diffs.Add(i);

                if (diffs.Count == 0) continue;

                _out.WriteLine($"  unit {u,2}: {diffs.Count} byte(s) differ");
                // Show offsets + before/after; cap output for non-name regions.
                int shown = 0;
                foreach (var off in diffs)
                {
                    if (shown++ > 64)
                    {
                        _out.WriteLine($"    ... ({diffs.Count - 64} more diffs suppressed)");
                        break;
                    }
                    var section = OffsetLabel(off);
                    _out.WriteLine($"    0x{off:X3} ({section}): a=0x{aBytes[off]:X2} b=0x{bBytes[off]:X2}");
                }
            }
            _out.WriteLine("");
        }

        Assert.True(true, "Informational dump.");
    }

    private static string OffsetLabel(int unitOffset) => unitOffset switch
    {
        < 0x0E => "identity",
        < 0x1C => "equip",
        < 0x20 => "exp/lvl/brv/fth",
        < 0x32 => "stat-base",
        < 0x74 => "ability_flag",
        < 0x80 => "job_level",
        < 0xAE => "job_point",
        < 0xDC => "total_jp",
        < 0x11C => "chr_name",
        < 0x126 => "name+slot-meta",
        < 0x22E => "equip_set",
        _ => "trailing"
    };

    [Fact]
    public void T3_guest_slot_51_byte_evolution()
    {
        // For every populated slot, dump unit slot 51's identity bytes + a hex view.
        // Then byte-level diff slot 51 between the latest "Argath-in-party" slot
        // and the earliest "Argath-gone" slot (per user: idx 3 first appearance,
        // idx 9 confirmed gone). Anything that changes is a candidate for the
        // active-flag.
        var save = Load();
        var populated = PopulatedSlotIndices(save);

        _out.WriteLine("=== Slot 51 (Argath / guest position) per-save snapshot ===");
        _out.WriteLine("save  char  UnitIdx  job   sex   level  exp  UnitNicknameRaw[0..15]");
        _out.WriteLine("----  ----  -------  ----  ----  -----  ---  ----------------------------------");
        foreach (var s in populated)
        {
            var unit = save.Slots[s].SaveWork.Battle.Units[51];
            var raw = unit.UnitNicknameRaw;
            var hex = Hex(raw.AsSpan(0, 16));
            _out.WriteLine(
                $"{s,4}  0x{unit.Character:X2}  0x{unit.UnitIndex:X2}    {unit.Job,3}   0x{unit.Sex:X2}  {unit.Level,5}  {unit.Exp,3}  {hex}");
        }
        _out.WriteLine("");

        // Pairwise diffs across consecutive populated saves on slot 51.
        for (int p = 0; p < populated.Length - 1; p++)
        {
            var aIdx = populated[p];
            var bIdx = populated[p + 1];
            var aUnit = save.Slots[aIdx].SaveWork.Battle.Units[51];
            var bUnit = save.Slots[bIdx].SaveWork.Battle.Units[51];

            var aBytes = new byte[600];
            var bBytes = new byte[600];
            aUnit.WriteTo(aBytes);
            bUnit.WriteTo(bBytes);

            var diffs = new List<int>();
            for (int i = 0; i < 600; i++)
                if (aBytes[i] != bBytes[i]) diffs.Add(i);

            _out.WriteLine($"### slot 51 diff: save {aIdx} → save {bIdx}  ({diffs.Count} byte(s) differ)");
            int shown = 0;
            foreach (var off in diffs)
            {
                if (shown++ > 48)
                {
                    _out.WriteLine($"    ... ({diffs.Count - 48} more diffs suppressed)");
                    break;
                }
                _out.WriteLine($"    0x{off:X3} ({OffsetLabel(off)}): a=0x{aBytes[off]:X2} b=0x{bBytes[off]:X2}");
            }
            _out.WriteLine("");
        }

        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T3b_section_level_diff_for_argath_transition()
    {
        // If the guest-departure flag lives outside UnitSaveData, partition the
        // full SaveWork bytes by section and diff slot-3 (Argath joins) against
        // every later save. We're looking for a section that changes consistently
        // across "Argath in party → Argath gone" — and that *isn't* expected to
        // drift between unrelated saves.
        var save = Load();
        var populated = PopulatedSlotIndices(save);

        // Per-save section drift relative to the user-attested "Argath joined" save (idx 3).
        const int anchorIdx = 3;
        if (Array.IndexOf(populated, anchorIdx) < 0)
        {
            _out.WriteLine($"WARN: anchor slot {anchorIdx} not populated; falling back to first populated slot {populated[0]}.");
        }
        var actualAnchor = Array.IndexOf(populated, anchorIdx) < 0 ? populated[0] : anchorIdx;

        var anchorBytes = save.Slots[actualAnchor].SaveWork.RawBytes;
        _out.WriteLine($"Anchor: slot {actualAnchor} (Argath in-party expected per user)");
        _out.WriteLine("");
        _out.WriteLine("                                                                        section diff counts (bytes that differ vs anchor)");
        var header = "save  ";
        foreach (var (name, _, _) in SectionMap) header += $"{name,-16} ";
        _out.WriteLine(header);

        foreach (var s in populated)
        {
            if (s == actualAnchor) continue;
            var bytes = save.Slots[s].SaveWork.RawBytes;
            var line = $"{s,4}  ";
            foreach (var (_, off, size) in SectionMap)
            {
                int count = 0;
                for (int i = 0; i < size; i++)
                    if (anchorBytes[off + i] != bytes[off + i]) count++;
                line += $"{count,-16} ";
            }
            _out.WriteLine(line);
        }
        _out.WriteLine("");
        _out.WriteLine("(Read: which section first jumps when Argath transitions out? That's our");
        _out.WriteLine(" candidate. Cross-reference with T3's slot-51 in-Battle diff to localize.)");

        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T5_UnitIndex_is_slot_index_for_active_units()
    {
        // The slot-51 finding (UnitIndex 0x33 == 51 when active, 0xFF when departed)
        // generalizes only if regular party slots 0..6 also carry their slot index
        // in UnitIndex when active. Format-notes line 253 claims "0..6 for regulars";
        // confirm against this fixture.
        var save = Load();
        var populated = PopulatedSlotIndices(save);

        _out.WriteLine("save  unit  char  UnitIndex  (UnitIndex == unit-slot?)");
        _out.WriteLine("----  ----  ----  ---------  ------------------------");
        foreach (var s in populated)
        {
            var battle = save.Slots[s].SaveWork.Battle;
            for (int u = 0; u <= 6; u++)
            {
                var unit = battle.Units[u];
                var matchNote = unit.IsEmpty ? "(empty)"
                              : unit.UnitIndex == u ? "MATCH"
                              : unit.UnitIndex == 0xFF ? "0xFF"
                              : "OTHER";
                _out.WriteLine($"{s,4}  {u,4}  0x{unit.Character:X2}  0x{unit.UnitIndex:X2}    {matchNote}");
            }
            _out.WriteLine("");
        }

        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T4_ramza_nickname_and_slot51_UnitIndex_consistency()
    {
        // Quick edges:
        //   - Ramza (idx 0) UnitNicknameRaw should be zero across all 10 slots
        //     (story characters can't be renamed in-game per user 2026-04-30).
        //   - Slot 51 UnitIndex behaviour: per fft-save-format-notes.md:253,
        //     UnitIndex is "0xFF for guests/empty". Confirm whether it stays
        //     0xFF when Argath is in party, or if it transitions on departure.
        var save = Load();
        var populated = PopulatedSlotIndices(save);

        _out.WriteLine("=== Ramza (unit 0) UnitNicknameRaw byte 0 across saves ===");
        foreach (var s in populated)
        {
            var unit = save.Slots[s].SaveWork.Battle.Units[0];
            var raw = unit.UnitNicknameRaw;
            var anyNonZero = raw.Any(b => b != 0);
            _out.WriteLine($"  slot {s}: char=0x{unit.Character:X2}  UnitNicknameRaw any-nonzero={anyNonZero}  byte[0]=0x{raw[0]:X2}");
        }
        _out.WriteLine("");

        _out.WriteLine("=== Slot 51 UnitIndex byte across saves ===");
        foreach (var s in populated)
        {
            var unit = save.Slots[s].SaveWork.Battle.Units[51];
            _out.WriteLine($"  slot {s}: char=0x{unit.Character:X2}  UnitIndex=0x{unit.UnitIndex:X2}");
        }

        Assert.True(true, "Informational dump.");
    }
}

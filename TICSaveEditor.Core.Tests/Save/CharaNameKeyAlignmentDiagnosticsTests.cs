using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;
using Xunit.Abstractions;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Diagnostic-only tests against <c>SaveFiles/enhanced.png</c> targeting the
/// 0x230 conflict: Nenkai's <c>FFTIVC_GameSaveData.bt</c> labels the bytes at
/// UnitSaveData offset 0x230 as <c>chara_name_key</c> (u16); the community-authored
/// TIC struct (<c>Docs/Unit Save Data and Battle Units.txt</c>) labels them as
/// <c>VoiceID</c> (4-byte int). Current Core code reads u16 and the M10 unit-list
/// cascade resolves names via <c>CharaName</c>. This battery dumps signals for
/// empirical disambiguation:
///
///   - bytes [0x230..0x233] raw + u16-LE + u32-LE decodings per populated unit
///   - high-bytes-zero distribution (universally zero supports u16 + Pad)
///   - u16-as-NameNo and u32-cast-to-ushort lookup hit rates against CharaName
///   - identification of "rename-empty + NameNo-zero" units (where CharaNameKey is
///     the only available name source — the discriminating cohort)
///
/// Tests pass trivially; xUnit ITestOutputHelper output is the deliverable.
/// Plan: C:/Users/SHODAN/.claude/plans/what-s-next-up-peppy-flamingo.md (Task 1).
/// </summary>
public class CharaNameKeyAlignmentDiagnosticsTests
{
    private readonly ITestOutputHelper _out;

    public CharaNameKeyAlignmentDiagnosticsTests(ITestOutputHelper output) => _out = output;

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

    private readonly record struct UnitSnapshot(
        int SlotIndex,
        int UnitIndex,
        byte Character,
        byte Sex,
        byte Job,
        ushort NameNo,
        byte B0, byte B1, byte B2, byte B3,
        bool RenameEmpty)
    {
        public ushort U16 => (ushort)(B0 | (B1 << 8));
        public uint U32 => (uint)(B0 | (B1 << 8) | (B2 << 16) | (B3 << 24));
        public ushort HighU16 => (ushort)(B2 | (B3 << 8));
        public bool HighZero => B2 == 0 && B3 == 0;
    }

    private static IEnumerable<UnitSnapshot> Snapshots(ManualSaveFile save)
    {
        var populated = PopulatedSlotIndices(save);
        foreach (var s in populated)
        {
            var battle = save.Slots[s].SaveWork.Battle;
            for (int u = 0; u < battle.Units.Count; u++)
            {
                var unit = battle.Units[u];
                if (unit.IsEmpty) continue;

                var bytes = new byte[600];
                unit.WriteTo(bytes);
                var renameEmpty = unit.UnitNicknameRaw[0] == 0;

                yield return new UnitSnapshot(
                    s, u,
                    unit.Character, unit.Sex, unit.Job, unit.NameNo,
                    bytes[0x230], bytes[0x231], bytes[0x232], bytes[0x233],
                    renameEmpty);
            }
        }
    }

    [Fact]
    public void T1_dump_0x230_decodings_with_charaname_lookups()
    {
        var save = Load();
        var ctx = new GameDataLoader().LoadBundled("en");

        _out.WriteLine("Per-unit decoding of bytes at UnitSaveData offset 0x230..0x233.");
        _out.WriteLine("u16 = bytes[0..1] LE; u32 = bytes[0..3] LE; high = bytes[2..3] LE.");
        _out.WriteLine("name(u16) = CharaName table lookup using u16 as NameNo.");
        _out.WriteLine("name(u32-low) = same lookup using (ushort)u32 (i.e. low 16 bits) — equals u16 by definition.");
        _out.WriteLine("");
        _out.WriteLine("slot unit char job NameNo  b0 b1 b2 b3   u16     u32        high  name(u16)");
        _out.WriteLine("---- ---- ---- --- ------  -----------   -----   ---------- ----  ------------------------------");
        foreach (var s in Snapshots(save))
        {
            var u16Hit = ctx.TryGetCharacterName(s.U16, out var info);
            var name = u16Hit && info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : "(no entry)";
            _out.WriteLine(
                $"{s.SlotIndex,4} {s.UnitIndex,4} 0x{s.Character:X2} {s.Job,3} {s.NameNo,6}  " +
                $"{s.B0:X2} {s.B1:X2} {s.B2:X2} {s.B3:X2}   " +
                $"{s.U16,5}   {s.U32,10}  {s.HighU16:X4}  {name}");
        }

        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T2_aggregate_signals()
    {
        var save = Load();
        var ctx = new GameDataLoader().LoadBundled("en");

        var snaps = Snapshots(save).ToList();
        int total = snaps.Count;
        int highZero = snaps.Count(s => s.HighZero);
        int highNonZero = total - highZero;

        int u16TableHit = snaps.Count(s =>
        {
            ctx.TryGetCharacterName(s.U16, out var info);
            return info != null && !string.IsNullOrEmpty(info.Name);
        });
        int u16Zero = snaps.Count(s => s.U16 == 0);

        var distinctU16 = snaps.Select(s => s.U16).Distinct().Count();
        var distinctU32 = snaps.Select(s => s.U32).Distinct().Count();

        _out.WriteLine("=== aggregate signals across all populated units ===");
        _out.WriteLine($"  total populated unit observations:   {total}");
        _out.WriteLine($"  high-bytes-zero (b2==0 && b3==0):   {highZero}  ({100.0 * highZero / Math.Max(1, total):F1}%)");
        _out.WriteLine($"  high-bytes-nonzero:                  {highNonZero}");
        _out.WriteLine($"  u16 == 0 (no key set):               {u16Zero}");
        _out.WriteLine($"  u16 hits CharaName table (non-empty Name): {u16TableHit}");
        _out.WriteLine($"  distinct u16 values:                 {distinctU16}");
        _out.WriteLine($"  distinct u32 values:                 {distinctU32}");
        _out.WriteLine("");
        _out.WriteLine("Reading guide:");
        _out.WriteLine("  - high-bytes universally zero AND distinct u16 ≈ distinct u32");
        _out.WriteLine("    → consistent with u16 CharaNameKey + 2 bytes Pad (Nenkai).");
        _out.WriteLine("  - any high-bytes-nonzero observation refutes 'always Pad' AND requires");
        _out.WriteLine("    that the high bytes carry meaning → consistent with u32 VoiceID (community).");
        _out.WriteLine("  - u16-table-hit-rate near total → values lie in a populated NameNo range (supports u16).");
        _out.WriteLine("  - u16-table-hit-rate ~zero with values clustered in odd ranges → either u32 or");
        _out.WriteLine("    a different table (would refute u16 CharaNameKey too).");

        if (highNonZero > 0)
        {
            _out.WriteLine("");
            _out.WriteLine("Units with high-bytes nonzero (b2/b3) — examine for u32 evidence:");
            foreach (var s in snaps.Where(s => !s.HighZero))
                _out.WriteLine($"  slot {s.SlotIndex,2} unit {s.UnitIndex,2}: b0..b3 = {s.B0:X2} {s.B1:X2} {s.B2:X2} {s.B3:X2}  u32={s.U32}");
        }

        Assert.True(true, "Informational dump.");
    }

    [Fact]
    public void T3_discriminating_cohort_no_rename_no_nameno()
    {
        // The cohort that depends solely on CharaNameKey for naming today:
        // UnitNicknameRaw[0] == 0  AND  NameNo == 0.  For these units, whatever
        // sits at 0x230 IS the visible name source. If u16 lookup produces a name
        // matching what the in-editor cascade renders, u16 is right. If u16
        // produces nonsense for these units, suspect u32 or a different field.
        var save = Load();
        var ctx = new GameDataLoader().LoadBundled("en");

        var snaps = Snapshots(save).Where(s => s.RenameEmpty && s.NameNo == 0).ToList();
        _out.WriteLine($"Discriminating cohort (rename-empty AND NameNo==0): {snaps.Count} observations.");
        _out.WriteLine("");
        _out.WriteLine("slot unit char job  raw[0..3]    u16   u32      high   name(u16)");
        _out.WriteLine("---- ---- ---- ---  -----------  ----  -------- -----  ------------------------");
        foreach (var s in snaps)
        {
            ctx.TryGetCharacterName(s.U16, out var info);
            var name = info != null && !string.IsNullOrEmpty(info.Name) ? info.Name : "(no entry)";
            _out.WriteLine(
                $"{s.SlotIndex,4} {s.UnitIndex,4} 0x{s.Character:X2} {s.Job,3}  " +
                $"{s.B0:X2} {s.B1:X2} {s.B2:X2} {s.B3:X2}    " +
                $"{s.U16,4}  {s.U32,8} {s.HighU16:X4}   {name}");
        }
        _out.WriteLine("");
        _out.WriteLine("If `name(u16)` for this cohort matches the in-editor display name (per UnitListItemViewModel),");
        _out.WriteLine("u16 CharaNameKey is correct. If `name(u16)` is junk while a u32 lookup against some other table");
        _out.WriteLine("would resolve it, community VoiceID is in play.");

        Assert.True(true, "Informational dump.");
    }
}

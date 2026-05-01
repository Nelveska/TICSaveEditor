using System.Buffers.Binary;
using TICSaveEditor.Core.Save;
using Xunit.Abstractions;

namespace TICSaveEditor.Core.Tests.Save;

/// <summary>
/// Diagnostic against <c>SaveFiles/Baseline/enhanced.png</c> to locate the real
/// in-game playtime offset within <c>InfoSection</c>. Per
/// <c>decisions_m10_smokefix_playtime_heroname_findings.md</c>, the current
/// <c>InfoSection.PlaytimeOffset = 0x74</c> reads zero for every populated slot
/// — a real bug. The user's Baseline file has 7+ populated slots spanning
/// &lt;1 hr to &gt;5 hrs of playtime, so we can SaveDiff intra-file (slot N vs
/// slot M) without a new fixture.
///
/// Strategy: for every byte offset 0..InfoSize-4 within InfoSection, read int32
/// LE and dump per-slot values sorted by SaveTimestamp. Flag offsets where
/// (a) all per-slot deltas are non-negative (monotonic) and (b) the total
/// range is in seconds-of-playtime magnitude (~0..50,000+ for 0..14 hrs).
///
/// Test passes trivially; xUnit ITestOutputHelper output is the deliverable.
/// </summary>
public class PlaytimeOffsetDiagnosticsTests
{
    private readonly ITestOutputHelper _out;

    public PlaytimeOffsetDiagnosticsTests(ITestOutputHelper output) => _out = output;

    private const int InfoSize = 0x00B8; // 184 bytes; matches SaveWorkLayout.InfoSize

    [Fact]
    public void T1_scan_InfoSection_for_monotonic_int32_field_correlating_with_playtime()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", "Baseline", "enhanced.png");
        var bytes = File.ReadAllBytes(path);
        var save = (ManualSaveFile)SaveFileLoader.Load(bytes, path);

        // Capture (slotIndex, SaveTimestamp, infoBytes[184]) for each populated slot.
        var rows = new List<(int Idx, DateTimeOffset Ts, byte[] Info)>();
        for (int i = 0; i < save.Slots.Count; i++)
        {
            if (save.Slots[i].IsEmpty) continue;
            var info = save.Slots[i].SaveWork.Info;
            var buf = new byte[info.Size];
            info.WriteTo(buf);
            rows.Add((i, save.Slots[i].SaveTimestamp, buf));
        }

        rows.Sort((a, b) => a.Ts.CompareTo(b.Ts));

        _out.WriteLine($"Loaded {rows.Count} populated slots, sorted by SaveTimestamp:");
        for (int r = 0; r < rows.Count; r++)
        {
            var (idx, ts, _) = rows[r];
            _out.WriteLine($"  row {r}: slot {idx,2}  ts={ts:O}");
        }
        _out.WriteLine("");

        // Score every byte offset 0..InfoSize-4 by:
        //   - monotonic non-negative across rows (min-delta >= 0)
        //   - magnitude fit (max value > 0 AND max value < 1_000_000 sec for sanity)
        //   - non-trivial (max value differs from min value)
        // Output: top candidates ranked by max-min range descending.
        var candidates = new List<(int Off, long[] Vals, long MinDelta, long Range, bool Aligned)>();
        for (int off = 0; off + 4 <= InfoSize; off++)
        {
            var vals = new long[rows.Count];
            for (int r = 0; r < rows.Count; r++)
                vals[r] = (long)BinaryPrimitives.ReadInt32LittleEndian(rows[r].Info.AsSpan(off, 4));

            // Compute min delta (negative if not monotonic) and overall range.
            long minDelta = long.MaxValue;
            for (int r = 1; r < rows.Count; r++)
                minDelta = Math.Min(minDelta, vals[r] - vals[r - 1]);
            long min = vals.Min(), max = vals.Max();

            bool monotonic = minDelta >= 0;
            bool nontrivial = max > min;
            bool inRange = max < 1_000_000L; // < ~277 hrs of seconds; rules out timestamps
            bool greaterThanZero = max > 0;

            if (monotonic && nontrivial && inRange && greaterThanZero)
                candidates.Add((off, vals, minDelta, max - min, off % 4 == 0));
        }

        candidates.Sort((a, b) => b.Range.CompareTo(a.Range));

        _out.WriteLine($"Found {candidates.Count} monotonic non-negative int32 LE candidates with max < 1,000,000.");
        _out.WriteLine("Showing top 15 by range (max - min):");
        _out.WriteLine("");
        _out.WriteLine("offset  aligned  min       max       range     min-delta  per-slot values...");
        _out.WriteLine("------  -------  --------  --------  --------  ---------  --------------------");
        for (int c = 0; c < Math.Min(15, candidates.Count); c++)
        {
            var (off, vals, mindelta, range, aligned) = candidates[c];
            var valsStr = string.Join(" ", vals.Select(v => v.ToString().PadLeft(7)));
            _out.WriteLine($" 0x{off:X3}    {(aligned ? "Y" : "N"),-3}      {vals.Min(),8}  {vals.Max(),8}  {range,8}  {mindelta,9}    [{valsStr}]");
        }

        _out.WriteLine("");
        _out.WriteLine("Interpretation guide:");
        _out.WriteLine("  - Real playtime in seconds: range should be thousands (e.g., 30 min = 1800; 5 hrs = 18,000)");
        _out.WriteLine("  - Aligned (offset % 4 == 0) is more likely than unaligned");
        _out.WriteLine("  - Min-delta should be non-negative AND ideally positive (each later save has more playtime)");
        _out.WriteLine("  - The current PlaytimeOffset = 0x74 should appear in this list iff it works; if it's missing, that confirms it's wrong");

        // Also dump the value at the current PlaytimeOffset (0x74) for context.
        _out.WriteLine("");
        _out.WriteLine("Current InfoSection.PlaytimeOffset (0x74) per-slot int32 LE values:");
        for (int r = 0; r < rows.Count; r++)
        {
            var v = BinaryPrimitives.ReadInt32LittleEndian(rows[r].Info.AsSpan(0x74, 4));
            _out.WriteLine($"  row {r}: slot {rows[r].Idx,2}  value=0x{v:X8} ({v})");
        }

        Assert.True(true, "Informational dump.");
    }
}

using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Operations;

public static class PartyOperations
{
    // Bulk ops gate on `IsInActiveParty(slotIndex)` — per
    // decisions_unit_index_active_flag.md, a unit slot can hold non-empty data
    // (Character != 0) yet not be in the current party (UnitIndex == 0xFF, e.g.
    // departed guests like Argath, dismissed recruits). Bulk ops skip those.

    private static string SkipReason(UnitSaveData u, int slotIndex)
    {
        if (u.IsEmpty) return "is empty";
        // Inactive but populated — the unit data persists for the game's own
        // bookkeeping (re-recruitable guests, dismissed-but-not-yet-overwritten
        // recruits). Bulk ops shouldn't touch them; that would mutate state the
        // user can't see in the in-game UI.
        return "is not in the active party (departed guest, dismissed recruit, or otherwise stowed)";
    }

    public static OperationResult SetAllToLevel(
        SaveWork saveWork,
        int level,
        IOperationProgress? progress = null)
    {
        if (saveWork is null) throw new ArgumentNullException(nameof(saveWork));

        return OperationRunner.Run(
            saveWork,
            validate: sw =>
            {
                var issues = new List<OperationIssue>();
                if (level < 1 || level > 99)
                    issues.Add(new OperationIssue(
                        $"Level must be in [1, 99] (got {level}).",
                        OperationSeverity.Error));

                for (int i = 0; i < sw.Battle.Units.Count; i++)
                {
                    if (!sw.Battle.IsActive(i))
                        issues.Add(new OperationIssue(
                            $"Unit slot {i} {SkipReason(sw.Battle.Units[i], i)}; will be skipped.",
                            OperationSeverity.Warning));
                }
                return issues;
            },
            apply: (sw, p) =>
            {
                int affected = 0;
                int total = sw.Battle.Units.Count;
                for (int i = 0; i < total; i++)
                {
                    var unit = sw.Battle.Units[i];
                    if (sw.Battle.IsActive(i))
                    {
                        unit.Level = (byte)level;
                        affected++;
                    }
                    p?.Report(new OperationProgressUpdate(i + 1, total, $"Unit {i}"));
                }
                return affected;
            },
            progress);
    }

    public static OperationResult MaxAllJobPoints(
        SaveWork saveWork,
        IOperationProgress? progress = null)
    {
        if (saveWork is null) throw new ArgumentNullException(nameof(saveWork));

        return OperationRunner.Run(
            saveWork,
            validate: sw =>
            {
                var issues = new List<OperationIssue>();
                for (int i = 0; i < sw.Battle.Units.Count; i++)
                {
                    if (!sw.Battle.IsActive(i))
                        issues.Add(new OperationIssue(
                            $"Unit slot {i} {SkipReason(sw.Battle.Units[i], i)}; will be skipped.",
                            OperationSeverity.Warning));
                }
                return issues;
            },
            apply: (sw, p) =>
            {
                int affected = 0;
                int total = sw.Battle.Units.Count;
                for (int i = 0; i < total; i++)
                {
                    var unit = sw.Battle.Units[i];
                    if (sw.Battle.IsActive(i))
                    {
                        unit.MaxAllJobPoints();
                        affected++;
                    }
                    p?.Report(new OperationProgressUpdate(i + 1, total, $"Unit {i}"));
                }
                return affected;
            },
            progress);
    }

    public static OperationResult LearnAllAbilitiesCurrentJob(
        SaveWork saveWork,
        IOperationProgress? progress = null)
    {
        if (saveWork is null) throw new ArgumentNullException(nameof(saveWork));

        // Job-byte → ability_flag slot mapping is class-name-based, not formulaic:
        // canonical generics (Squire..Mime + Dark/Onion Knight) get their own slot;
        // story-unique classes (Holy Knight, Sword Saint, Machinist, etc.) all share
        // slot 0 with Squire. The truly unmappable cases are monsters, "Unknown"
        // placeholders, the no-job sentinel, and out-of-range bytes — those get
        // skipped with a warning.
        // UnitSaveData.GetAbilityFlagSlotForJob owns the table.
        return OperationRunner.Run(
            saveWork,
            validate: sw =>
            {
                var issues = new List<OperationIssue>();
                for (int i = 0; i < sw.Battle.Units.Count; i++)
                {
                    var u = sw.Battle.Units[i];
                    if (!sw.Battle.IsActive(i))
                    {
                        issues.Add(new OperationIssue(
                            $"Unit slot {i} {SkipReason(u, i)}; will be skipped.",
                            OperationSeverity.Warning));
                    }
                    else if (UnitSaveData.GetAbilityFlagSlotForJob(u.Job) is null)
                    {
                        issues.Add(new OperationIssue(
                            $"Unit slot {i} has job 0x{u.Job:X2} (monster, placeholder, or unsupported class); ability flags not stored for this job; will be skipped.",
                            OperationSeverity.Warning));
                    }
                }
                return issues;
            },
            apply: (sw, p) =>
            {
                int affected = 0;
                int total = sw.Battle.Units.Count;
                for (int i = 0; i < total; i++)
                {
                    var unit = sw.Battle.Units[i];
                    if (sw.Battle.IsActive(i) && unit.TryLearnAllAbilitiesForCurrentJob())
                    {
                        affected++;
                    }
                    p?.Report(new OperationProgressUpdate(i + 1, total, $"Unit {i}"));
                }
                return affected;
            },
            progress);
    }
}

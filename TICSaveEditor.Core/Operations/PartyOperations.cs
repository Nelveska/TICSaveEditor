using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Operations;

public static class PartyOperations
{
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
                    if (sw.Battle.Units[i].IsEmpty)
                        issues.Add(new OperationIssue(
                            $"Unit slot {i} is empty; will be skipped.",
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
                    if (!unit.IsEmpty)
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
                    if (sw.Battle.Units[i].IsEmpty)
                        issues.Add(new OperationIssue(
                            $"Unit slot {i} is empty; will be skipped.",
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
                    if (!unit.IsEmpty)
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

        // No-job sentinel warning is deferred per decisions_m8_no_job_validation_deferred.md;
        // empty-unit warning is sufficient v0.1 coverage. If a populated unit's Job byte is
        // out of the 22-job ability-flag array range, UnitSaveData.LearnAllAbilitiesForJob
        // throws ArgumentOutOfRangeException, which OperationRunner catches → restores the
        // snapshot → returns UnexpectedFailure.
        return OperationRunner.Run(
            saveWork,
            validate: sw =>
            {
                var issues = new List<OperationIssue>();
                for (int i = 0; i < sw.Battle.Units.Count; i++)
                {
                    if (sw.Battle.Units[i].IsEmpty)
                        issues.Add(new OperationIssue(
                            $"Unit slot {i} is empty; will be skipped.",
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
                    if (!unit.IsEmpty)
                    {
                        unit.LearnAllAbilitiesForJob(unit.Job);
                        affected++;
                    }
                    p?.Report(new OperationProgressUpdate(i + 1, total, $"Unit {i}"));
                }
                return affected;
            },
            progress);
    }
}

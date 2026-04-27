using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Operations;

public static class SlotOperations
{
    private const int MaxSlotIndex = ManualSaveFile.SlotCount - 1;
    private const int MaxUnitIndex = 53; // BattleSection.UnitCount - 1

    /// <summary>Copies a unit from one slot to another. Per spec §9.2/§9.4.</summary>
    public static OperationResult CopyCharacter(
        ManualSaveFile file,
        int sourceSlot, int sourceUnitIndex,
        int destSlot, int destUnitIndex,
        IOperationProgress? progress = null)
        => CopyOrDuplicate(file, sourceSlot, sourceUnitIndex, destSlot, destUnitIndex, progress);

    /// <summary>
    /// Alias for <see cref="CopyCharacter"/>. Spec §9.2 lists both names with identical
    /// signatures; per <c>decisions_m8_duplicate_character_alias.md</c> they're the same op.
    /// </summary>
    public static OperationResult DuplicateCharacter(
        ManualSaveFile file,
        int sourceSlot, int sourceUnitIndex,
        int destSlot, int destUnitIndex,
        IOperationProgress? progress = null)
        => CopyOrDuplicate(file, sourceSlot, sourceUnitIndex, destSlot, destUnitIndex, progress);

    private static OperationResult CopyOrDuplicate(
        ManualSaveFile file,
        int sourceSlot, int sourceUnitIndex,
        int destSlot, int destUnitIndex,
        IOperationProgress? progress)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));

        return OperationRunner.Run(
            file,
            validate: f =>
            {
                var issues = new List<OperationIssue>();
                if (!ValidateSlotRange(sourceSlot, "sourceSlot", issues)) return issues;
                if (!ValidateSlotRange(destSlot, "destSlot", issues)) return issues;
                if (!ValidateUnitRange(sourceUnitIndex, "sourceUnitIndex", issues)) return issues;
                if (!ValidateUnitRange(destUnitIndex, "destUnitIndex", issues)) return issues;

                var srcWork = f.Slots[sourceSlot].SaveWork;
                if (srcWork is null)
                {
                    issues.Add(new OperationIssue(
                        $"Source slot {sourceSlot} has no SaveWork.",
                        OperationSeverity.Error));
                    return issues;
                }

                var srcUnit = srcWork.Battle.Units[sourceUnitIndex];
                if (srcUnit.IsEmpty)
                {
                    issues.Add(new OperationIssue(
                        $"Source unit (slot {sourceSlot}, index {sourceUnitIndex}) is empty.",
                        OperationSeverity.Error));
                    return issues;
                }

                var destWork = f.Slots[destSlot].SaveWork;
                if (destWork is not null)
                {
                    var destUnit = destWork.Battle.Units[destUnitIndex];
                    if (!destUnit.IsEmpty)
                    {
                        issues.Add(new OperationIssue(
                            $"Destination unit (slot {destSlot}, index {destUnitIndex}) is non-empty; will be overwritten.",
                            OperationSeverity.Warning));
                    }
                }
                return issues;
            },
            apply: (f, p) =>
            {
                p?.Report(new OperationProgressUpdate(0, 1, "Copying"));
                var srcUnit = f.Slots[sourceSlot].SaveWork!.Battle.Units[sourceUnitIndex];
                var destUnit = f.Slots[destSlot].SaveWork!.Battle.Units[destUnitIndex];

                var buffer = new byte[UnitSaveData.Size];
                srcUnit.WriteTo(buffer);
                destUnit.RehydrateFrom(buffer);

                p?.Report(new OperationProgressUpdate(1, 1, "Copying"));
                return 1;
            },
            progress);
    }

    /// <summary>
    /// Swaps two characters. Per spec §9.4: same range validation as CopyCharacter,
    /// BUT no source-empty error (swapping two empty units is a no-op).
    /// </summary>
    public static OperationResult SwapCharacter(
        ManualSaveFile file,
        int sourceSlot, int sourceUnitIndex,
        int destSlot, int destUnitIndex,
        IOperationProgress? progress = null)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));

        return OperationRunner.Run(
            file,
            validate: f =>
            {
                var issues = new List<OperationIssue>();
                ValidateSlotRange(sourceSlot, "sourceSlot", issues);
                ValidateSlotRange(destSlot, "destSlot", issues);
                ValidateUnitRange(sourceUnitIndex, "sourceUnitIndex", issues);
                ValidateUnitRange(destUnitIndex, "destUnitIndex", issues);
                return issues;
            },
            apply: (f, p) =>
            {
                p?.Report(new OperationProgressUpdate(0, 1, "Swapping"));
                var srcUnit = f.Slots[sourceSlot].SaveWork!.Battle.Units[sourceUnitIndex];
                var destUnit = f.Slots[destSlot].SaveWork!.Battle.Units[destUnitIndex];

                var srcBuffer = new byte[UnitSaveData.Size];
                var destBuffer = new byte[UnitSaveData.Size];
                srcUnit.WriteTo(srcBuffer);
                destUnit.WriteTo(destBuffer);
                destUnit.RehydrateFrom(srcBuffer);
                srcUnit.RehydrateFrom(destBuffer);

                p?.Report(new OperationProgressUpdate(1, 1, "Swapping"));
                return 2;
            },
            progress);
    }

    private static bool ValidateSlotRange(int value, string paramName, List<OperationIssue> issues)
    {
        if (value < 0 || value > MaxSlotIndex)
        {
            issues.Add(new OperationIssue(
                $"{paramName} must be in [0, {MaxSlotIndex}] (got {value}).",
                OperationSeverity.Error));
            return false;
        }
        return true;
    }

    private static bool ValidateUnitRange(int value, string paramName, List<OperationIssue> issues)
    {
        if (value < 0 || value > MaxUnitIndex)
        {
            issues.Add(new OperationIssue(
                $"{paramName} must be in [0, {MaxUnitIndex}] (got {value}).",
                OperationSeverity.Error));
            return false;
        }
        return true;
    }
}

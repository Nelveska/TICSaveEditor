namespace TICSaveEditor.Core.Operations;

/// <summary>
/// Runs an operation against an <see cref="ISnapshotable"/> + <see cref="ISuspendable"/> target
/// with snapshot-and-rollback semantics per spec §9.3.
///
/// Phase order: validate → snapshot → suspend → apply → on-exception-restore.
///
/// Validation rules of <see cref="OperationSeverity.Error"/> short-circuit before any
/// state mutation. Validation warnings (and below) are returned alongside the success
/// result. If <c>apply</c> throws after mutation has begun, the snapshot is restored
/// before the exception is wrapped in <see cref="OperationResult.UnexpectedFailure"/>.
/// </summary>
internal static class OperationRunner
{
    public static OperationResult Run<T>(
        T target,
        Func<T, IReadOnlyList<OperationIssue>> validate,
        Func<T, IOperationProgress?, int> apply,
        IOperationProgress? progress)
        where T : ISnapshotable, ISuspendable
    {
        if (target is null) throw new ArgumentNullException(nameof(target));
        if (validate is null) throw new ArgumentNullException(nameof(validate));
        if (apply is null) throw new ArgumentNullException(nameof(apply));

        var issues = validate(target);
        if (issues.Any(i => i.Severity == OperationSeverity.Error))
        {
            return OperationResult.ValidationFailed(issues);
        }

        object snapshot;
        try
        {
            snapshot = target.CreateSnapshot();
        }
        catch (Exception ex)
        {
            return OperationResult.UnexpectedFailure(ex, issues);
        }

        int affected;
        try
        {
            using (target.SuspendNotifications())
            {
                affected = apply(target, progress);
            }
        }
        catch (Exception ex)
        {
            try
            {
                target.RestoreFromSnapshot(snapshot);
            }
            catch
            {
                // If restore itself fails we surface the original apply exception;
                // the restore-failure exception is intentionally swallowed since the
                // caller's primary signal is "the apply phase did not succeed."
            }
            return OperationResult.UnexpectedFailure(ex, issues);
        }

        return OperationResult.Success(affected, issues);
    }
}

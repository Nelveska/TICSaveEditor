namespace TICSaveEditor.Core.Operations;

public record OperationResult(
    bool Succeeded,
    int UnitsAffected,
    IReadOnlyList<OperationIssue> Issues,
    Exception? Exception = null)
{
    public static OperationResult ValidationFailed(IReadOnlyList<OperationIssue> issues)
        => new(false, 0, issues);

    public static OperationResult Success(int affected, IReadOnlyList<OperationIssue> issues)
        => new(true, affected, issues);

    public static OperationResult UnexpectedFailure(Exception ex, IReadOnlyList<OperationIssue> issues)
        => new(false, 0, issues, ex);
}

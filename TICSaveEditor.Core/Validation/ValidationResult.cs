namespace TICSaveEditor.Core.Validation;

public record ValidationResult(IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsValid => Issues.All(i => i.Severity != ValidationSeverity.Error);
    public bool HasWarnings => Issues.Any(i => i.Severity == ValidationSeverity.Warning);

    public static ValidationResult Empty { get; } = new(Array.Empty<ValidationIssue>());
}

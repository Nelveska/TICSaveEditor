namespace TICSaveEditor.Core.Validation;

public record ValidationIssue(
    string FieldName,
    string Message,
    ValidationSeverity Severity = ValidationSeverity.Error);

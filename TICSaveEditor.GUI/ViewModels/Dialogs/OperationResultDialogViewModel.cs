using System.Collections.Generic;
using System.Text.RegularExpressions;
using TICSaveEditor.Core.Operations;

namespace TICSaveEditor.GUI.ViewModels.Dialogs;

public class OperationResultDialogViewModel : ViewModelBase
{
    // "Unit slot 7 is empty; will be skipped." -> capture digits.
    private static readonly Regex EmptyUnitPattern =
        new(@"^Unit slot (\d+) is empty; will be skipped\.$",
            RegexOptions.Compiled);

    // "Unit slot 4 has job 26 (story-character / out-of-range); ability flags not stored ..."
    private static readonly Regex OutOfRangeJobPattern =
        new(@"^Unit slot (\d+) has job (\d+) \(story-character / out-of-range\);.*$",
            RegexOptions.Compiled);

    public OperationResultDialogViewModel(string opLabel, OperationResult result)
    {
        OpLabel = opLabel;
        Title = $"{opLabel}: {(result.Succeeded ? "Done" : "Failed")}";
        IsSuccess = result.Succeeded;

        var emptySlots = new List<int>();
        var oorSlots = new List<int>();
        var otherIssues = new List<string>();
        foreach (var issue in result.Issues)
        {
            var emptyMatch = EmptyUnitPattern.Match(issue.Description);
            var oorMatch = OutOfRangeJobPattern.Match(issue.Description);
            if (issue.Severity == OperationSeverity.Warning && emptyMatch.Success)
            {
                emptySlots.Add(int.Parse(emptyMatch.Groups[1].Value));
            }
            else if (issue.Severity == OperationSeverity.Warning && oorMatch.Success)
            {
                oorSlots.Add(int.Parse(oorMatch.Groups[1].Value));
            }
            else
            {
                otherIssues.Add($"[{issue.Severity}] {issue.Description}");
            }
        }

        var grouped = new List<string>();
        if (emptySlots.Count > 0)
        {
            grouped.Add($"{emptySlots.Count} empty unit(s) skipped " +
                        $"(slots {string.Join(", ", emptySlots)}).");
        }
        if (oorSlots.Count > 0)
        {
            grouped.Add($"{oorSlots.Count} story-character unit(s) skipped — " +
                        $"ability flags not stored for non-generic jobs " +
                        $"(slots {string.Join(", ", oorSlots)}).");
        }
        grouped.AddRange(otherIssues);
        if (result.Exception is { } ex)
        {
            grouped.Add($"Exception: {ex.GetType().Name}: {ex.Message}");
        }

        GroupedIssues = grouped;
        Headline = result.Succeeded
            ? $"{opLabel}: applied to {result.UnitsAffected} unit(s)."
            : $"{opLabel} did not run.";
    }

    public string OpLabel { get; }
    public string Title { get; }
    public bool IsSuccess { get; }
    public string Headline { get; }
    public IReadOnlyList<string> GroupedIssues { get; }
}

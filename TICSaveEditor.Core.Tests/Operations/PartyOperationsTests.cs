using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Operations;

public class PartyOperationsTests
{
    private static SaveWork NewSaveWorkWithUnits(int populatedCount)
    {
        var sw = new SaveWork(new byte[SaveWork.Size]);
        for (int i = 0; i < populatedCount; i++)
        {
            sw.Battle.Units[i].Character = (byte)(i + 1); // make non-empty
            sw.Battle.Units[i].Job = 1;                   // valid job index for ability flags
        }
        return sw;
    }

    [Fact]
    public void SetAllToLevel_returns_validation_error_when_level_out_of_range()
    {
        var sw = NewSaveWorkWithUnits(3);
        var result = PartyOperations.SetAllToLevel(sw, level: 0);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, i => i.Severity == OperationSeverity.Error);
    }

    [Fact]
    public void SetAllToLevel_sets_level_on_populated_units_skips_empty()
    {
        var sw = NewSaveWorkWithUnits(3);
        var result = PartyOperations.SetAllToLevel(sw, 50);

        Assert.True(result.Succeeded);
        Assert.Equal(3, result.UnitsAffected);
        for (int i = 0; i < 3; i++)
            Assert.Equal((byte)50, sw.Battle.Units[i].Level);
        // Units beyond 3 are still empty.
        Assert.True(sw.Battle.Units[3].IsEmpty);
    }

    [Fact]
    public void SetAllToLevel_warns_for_each_empty_slot()
    {
        var sw = NewSaveWorkWithUnits(2);
        var result = PartyOperations.SetAllToLevel(sw, 50);

        var warnings = result.Issues.Count(i => i.Severity == OperationSeverity.Warning);
        Assert.Equal(54 - 2, warnings);
    }

    [Fact]
    public void MaxAllJobPoints_calls_unit_method_for_populated_units()
    {
        var sw = NewSaveWorkWithUnits(2);
        var result = PartyOperations.MaxAllJobPoints(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.UnitsAffected);
        // Verify by checking unit 0's job-point at index 0 is now ushort.MaxValue.
        Assert.Equal(ushort.MaxValue, sw.Battle.Units[0].GetJobPoint(0));
    }

    [Fact]
    public void MaxAllJobPoints_no_op_on_empty_party_returns_zero_affected()
    {
        var sw = new SaveWork(new byte[SaveWork.Size]);
        var result = PartyOperations.MaxAllJobPoints(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(0, result.UnitsAffected);
    }

    [Fact]
    public void LearnAllAbilitiesCurrentJob_succeeds_for_populated_units()
    {
        var sw = NewSaveWorkWithUnits(2);
        var result = PartyOperations.LearnAllAbilitiesCurrentJob(sw);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.UnitsAffected);
        // Job 1 abilities should now be all-set: ability flags entry at index 1, byte 0 = 0xFF.
        Assert.Equal(0xFF, sw.Battle.Units[0].GetAbilityFlagByte(1, 0));
    }

    [Fact]
    public void Operations_use_progress_when_provided()
    {
        var sw = NewSaveWorkWithUnits(3);
        var captured = new List<OperationProgressUpdate>();
        var progress = new TestProgress(captured);

        PartyOperations.SetAllToLevel(sw, 50, progress);

        // Progress reports once per unit slot (54 total).
        Assert.Equal(54, captured.Count);
        Assert.Equal(54, captured[^1].Total);
        Assert.Equal(54, captured[^1].Current);
    }

    [Fact]
    public void Operations_throw_on_null_save_work()
    {
        Assert.Throws<ArgumentNullException>(() => PartyOperations.SetAllToLevel(null!, 50));
        Assert.Throws<ArgumentNullException>(() => PartyOperations.MaxAllJobPoints(null!));
        Assert.Throws<ArgumentNullException>(() => PartyOperations.LearnAllAbilitiesCurrentJob(null!));
    }

    [Fact]
    public void SetAllToLevel_validation_error_does_not_mutate_state()
    {
        var sw = NewSaveWorkWithUnits(2);
        var originalLevel = sw.Battle.Units[0].Level;
        PartyOperations.SetAllToLevel(sw, level: 100); // > 99 = error
        Assert.Equal(originalLevel, sw.Battle.Units[0].Level);
    }

    private sealed class TestProgress : IOperationProgress
    {
        private readonly List<OperationProgressUpdate> _updates;
        public TestProgress(List<OperationProgressUpdate> u) => _updates = u;
        public void Report(OperationProgressUpdate u) => _updates.Add(u);
    }
}

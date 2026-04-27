using TICSaveEditor.Core.Operations;

namespace TICSaveEditor.Core.Tests.Operations;

public class OperationRunnerTests
{
    private sealed class TestTarget : ISnapshotable, ISuspendable
    {
        public int State { get; set; }
        public int CreateSnapshotCalls { get; private set; }
        public int RestoreCalls { get; private set; }
        public int SuspendCalls { get; private set; }
        public int SuspendDisposeCalls { get; private set; }

        public object CreateSnapshot()
        {
            CreateSnapshotCalls++;
            return State;
        }

        public void RestoreFromSnapshot(object snapshot)
        {
            RestoreCalls++;
            State = (int)snapshot;
        }

        public IDisposable SuspendNotifications()
        {
            SuspendCalls++;
            return new Scope(this);
        }

        private sealed class Scope : IDisposable
        {
            private readonly TestTarget _t;
            public Scope(TestTarget t) => _t = t;
            public void Dispose() => _t.SuspendDisposeCalls++;
        }
    }

    [Fact]
    public void Run_returns_ValidationFailed_when_validate_produces_error()
    {
        var target = new TestTarget();
        var result = OperationRunner.Run(
            target,
            validate: _ => new[] { new OperationIssue("bad", OperationSeverity.Error) },
            apply: (_, _) => 99,
            progress: null);

        Assert.False(result.Succeeded);
        Assert.Equal(0, result.UnitsAffected);
        Assert.Single(result.Issues);
        Assert.Equal(0, target.CreateSnapshotCalls);
        Assert.Equal(0, target.SuspendCalls);
    }

    [Fact]
    public void Run_does_not_short_circuit_on_warning_only_validation()
    {
        var target = new TestTarget();
        var result = OperationRunner.Run(
            target,
            validate: _ => new[] { new OperationIssue("hint", OperationSeverity.Warning) },
            apply: (_, _) => 1,
            progress: null);

        Assert.True(result.Succeeded);
        Assert.Single(result.Issues);
        Assert.Equal(1, target.CreateSnapshotCalls);
    }

    [Fact]
    public void Run_takes_snapshot_before_apply()
    {
        var target = new TestTarget { State = 42 };
        OperationRunner.Run(
            target,
            validate: _ => Array.Empty<OperationIssue>(),
            apply: (t, _) => { Assert.Equal(1, t.CreateSnapshotCalls); return 0; },
            progress: null);
    }

    [Fact]
    public void Run_opens_suspend_scope_around_apply()
    {
        var target = new TestTarget();
        OperationRunner.Run(
            target,
            validate: _ => Array.Empty<OperationIssue>(),
            apply: (t, _) =>
            {
                Assert.Equal(1, t.SuspendCalls);
                Assert.Equal(0, t.SuspendDisposeCalls);
                return 0;
            },
            progress: null);
        Assert.Equal(1, target.SuspendDisposeCalls);
    }

    [Fact]
    public void Run_restores_snapshot_on_apply_exception()
    {
        var target = new TestTarget { State = 100 };
        var result = OperationRunner.Run(
            target,
            validate: _ => Array.Empty<OperationIssue>(),
            apply: (t, _) => { t.State = 999; throw new InvalidOperationException("boom"); },
            progress: null);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Exception);
        Assert.Equal(100, target.State);
        Assert.Equal(1, target.RestoreCalls);
    }

    [Fact]
    public void Run_returns_Success_with_affected_count()
    {
        var target = new TestTarget();
        var result = OperationRunner.Run(
            target,
            validate: _ => Array.Empty<OperationIssue>(),
            apply: (_, _) => 7,
            progress: null);

        Assert.True(result.Succeeded);
        Assert.Equal(7, result.UnitsAffected);
    }

    [Fact]
    public void Run_throws_on_null_target()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OperationRunner.Run<TestTarget>(null!,
                validate: _ => Array.Empty<OperationIssue>(),
                apply: (_, _) => 0,
                progress: null));
    }

    [Fact]
    public void Run_throws_on_null_validate_or_apply()
    {
        var target = new TestTarget();
        Assert.Throws<ArgumentNullException>(() =>
            OperationRunner.Run(target, null!, (_, _) => 0, null));
        Assert.Throws<ArgumentNullException>(() =>
            OperationRunner.Run(target, _ => Array.Empty<OperationIssue>(), null!, null));
    }

    [Fact]
    public void Run_forwards_progress_to_apply_lambda()
    {
        var target = new TestTarget();
        var captured = new List<OperationProgressUpdate>();
        var progressSink = new TestProgress(captured);

        OperationRunner.Run(
            target,
            validate: _ => Array.Empty<OperationIssue>(),
            apply: (_, p) =>
            {
                p?.Report(new OperationProgressUpdate(1, 1, "done"));
                return 1;
            },
            progress: progressSink);

        Assert.Single(captured);
        Assert.Equal("done", captured[0].CurrentItem);
    }

    private sealed class TestProgress : IOperationProgress
    {
        private readonly List<OperationProgressUpdate> _updates;
        public TestProgress(List<OperationProgressUpdate> updates) => _updates = updates;
        public void Report(OperationProgressUpdate update) => _updates.Add(update);
    }

    [Fact]
    public void Run_supports_warning_only_passes_warnings_through()
    {
        var target = new TestTarget();
        var result = OperationRunner.Run(
            target,
            validate: _ => new[]
            {
                new OperationIssue("first", OperationSeverity.Warning),
                new OperationIssue("second", OperationSeverity.Info),
            },
            apply: (_, _) => 5,
            progress: null);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Issues.Count);
        Assert.Equal(5, result.UnitsAffected);
    }
}

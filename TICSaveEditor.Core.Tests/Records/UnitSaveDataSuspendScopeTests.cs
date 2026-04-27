using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataSuspendScopeTests
{
    [Fact]
    public void Suspend_coalesces_one_event_per_affected_collection()
    {
        var unit = new UnitSaveData(new byte[600]);
        var hits = new Dictionary<string, int>();
        unit.PropertyChanged += (_, e) =>
        {
            var name = e.PropertyName ?? "";
            hits[name] = hits.GetValueOrDefault(name) + 1;
        };

        // MaxAllJobPoints touches 23 entries inside one suspend scope.
        unit.MaxAllJobPoints();

        Assert.Equal(1, hits.GetValueOrDefault(nameof(UnitSaveData.JobPoints)));
    }

    [Fact]
    public void Suspend_with_no_mutation_fires_no_events_on_dispose()
    {
        var unit = new UnitSaveData(new byte[600]);
        var hits = 0;
        unit.PropertyChanged += (_, _) => hits++;

        // ForgetAllAbilities-on-already-zero is a no-op (no byte changes), but the suspend scope still wraps.
        unit.ForgetAllAbilities();

        Assert.Equal(0, hits);
    }

    [Fact]
    public void Suspend_supports_nested_bulk_ops_without_double_firing()
    {
        var unit = new UnitSaveData(new byte[600]);
        var jobPointHits = 0;
        var abilityFlagsHits = 0;
        unit.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UnitSaveData.JobPoints)) jobPointHits++;
            if (e.PropertyName == nameof(UnitSaveData.AbilityFlags)) abilityFlagsHits++;
        };

        // Two top-level bulk ops should each fire once.
        unit.MaxAllJobPoints();
        unit.LearnAllAbilities();

        Assert.Equal(1, jobPointHits);
        Assert.Equal(1, abilityFlagsHits);
    }
}

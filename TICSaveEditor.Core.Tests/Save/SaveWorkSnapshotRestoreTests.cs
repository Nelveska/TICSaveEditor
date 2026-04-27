using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveWorkSnapshotRestoreTests
{
    private static SaveWork NewSaveWork(int randomSeed)
    {
        var rng = new Random(randomSeed);
        var bytes = new byte[SaveWork.Size];
        rng.NextBytes(bytes);
        return new SaveWork(bytes);
    }

    [Fact]
    public void CreateSnapshot_returns_byte_array_of_size_TotalSize()
    {
        var sw = NewSaveWork(1);
        var snapshot = sw.CreateSnapshot();
        var bytes = Assert.IsType<byte[]>(snapshot);
        Assert.Equal(SaveWork.Size, bytes.Length);
    }

    [Fact]
    public void RestoreFromSnapshot_reverts_mutations()
    {
        var sw = NewSaveWork(2);
        var originalRawBytes = sw.RawBytes;
        var snapshot = sw.CreateSnapshot();

        // Mutate via Card.Title (simple editable field).
        sw.Card.Title = "Modified Title";
        Assert.Equal("Modified Title", sw.Card.Title);

        sw.RestoreFromSnapshot(snapshot);

        // After restore, RawBytes should equal the original.
        Assert.Equal(originalRawBytes, sw.RawBytes);
    }

    [Fact]
    public void RestoreFromSnapshot_throws_on_wrong_type()
    {
        var sw = NewSaveWork(3);
        Assert.Throws<ArgumentException>(() => sw.RestoreFromSnapshot("not a byte array"));
    }

    [Fact]
    public void RestoreFromSnapshot_throws_on_wrong_length()
    {
        var sw = NewSaveWork(4);
        Assert.Throws<ArgumentException>(() => sw.RestoreFromSnapshot(new byte[100]));
    }

    [Fact]
    public void SuspendNotifications_coalesces_section_property_changes()
    {
        var sw = NewSaveWork(5);
        // Pre-populate at least one unit so we have something to mutate.
        sw.Battle.Units[0].Character = 1;
        sw.Battle.Units[0].Job = 5;

        var unitsEvents = 0;
        sw.Battle.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BattleSection.Units)) unitsEvents++;
        };

        using (sw.SuspendNotifications())
        {
            // 3 mutations on the unit; without suspend each would bubble as a Units event.
            sw.Battle.Units[0].Level = 50;
            sw.Battle.Units[0].Exp = 100;
            sw.Battle.Units[0].StartFaith = 50;
        }

        // After suspend release, BattleSection should fire Units exactly once.
        Assert.Equal(1, unitsEvents);
    }

    [Fact]
    public void Nested_SuspendNotifications_only_releases_at_outer_dispose()
    {
        var sw = NewSaveWork(6);
        sw.Battle.Units[0].Character = 1;

        var unitsEvents = 0;
        sw.Battle.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BattleSection.Units)) unitsEvents++;
        };

        using (sw.SuspendNotifications())
        {
            using (sw.SuspendNotifications())
            {
                sw.Battle.Units[0].Level = 1;
            }
            // Inner dispose fires nothing; outer scope is still active.
            Assert.Equal(0, unitsEvents);
            sw.Battle.Units[0].Level = 2;
        }
        Assert.Equal(1, unitsEvents);
    }
}

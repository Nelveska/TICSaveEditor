using TICSaveEditor.Core.Records.Entries;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Records.Entries;

public class JobNewEntryTests
{
    [Fact]
    public void Value_round_trips_through_owner()
    {
        var sec = new FftoBattleSection(new byte[SaveWorkLayout.FftoBattleSize]);
        sec.JobNewFlags[3].Value = 0x42;

        Assert.Equal((byte)0x42, sec.GetJobNewFlag(3));
        Assert.Equal((byte)0x42, sec.JobNewFlags[3].Value);
    }

    [Fact]
    public void RaiseValueChanged_fires_PropertyChanged_for_Value()
    {
        var sec = new FftoBattleSection(new byte[SaveWorkLayout.FftoBattleSize]);
        var entry = sec.JobNewFlags[5];
        var hits = 0;
        entry.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(JobNewEntry.Value)) hits++;
        };

        sec.SetJobNewFlag(5, 0x10);

        Assert.Equal(1, hits);
    }
}

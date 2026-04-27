using TICSaveEditor.Core.Records.Entries;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Records.Entries;

public class JobDisableEntryTests
{
    [Fact]
    public void Value_round_trips_through_owner()
    {
        var sec = new FftoBattleSection(new byte[SaveWorkLayout.FftoBattleSize]);
        sec.JobDisableFlags[42].Value = 0x99;

        Assert.Equal((byte)0x99, sec.GetJobDisableFlag(42));
        Assert.Equal((byte)0x99, sec.JobDisableFlags[42].Value);
    }

    [Fact]
    public void RaiseValueChanged_fires_PropertyChanged_for_Value()
    {
        var sec = new FftoBattleSection(new byte[SaveWorkLayout.FftoBattleSize]);
        var entry = sec.JobDisableFlags[10];
        var hits = 0;
        entry.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(JobDisableEntry.Value)) hits++;
        };

        sec.SetJobDisableFlag(10, 0xFF);

        Assert.Equal(1, hits);
    }
}

using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveWorkLayoutTests
{
    [Fact]
    public void Layout_first_entry_starts_at_zero()
    {
        Assert.Equal(0, SaveWorkLayout.Entries[0].Offset);
    }

    [Fact]
    public void Layout_entries_have_no_gaps_or_overlaps()
    {
        for (var i = 1; i < SaveWorkLayout.Entries.Length; i++)
        {
            var prev = SaveWorkLayout.Entries[i - 1];
            var curr = SaveWorkLayout.Entries[i];
            Assert.True(
                prev.Offset + prev.Size == curr.Offset,
                $"Gap or overlap between '{prev.Name}' (ends at 0x{prev.Offset + prev.Size:X4}) " +
                $"and '{curr.Name}' (starts at 0x{curr.Offset:X4}).");
        }
    }

    [Fact]
    public void Layout_total_equals_savework_size()
    {
        var last = SaveWorkLayout.Entries[^1];
        var coveredEnd = last.Offset + last.Size;

        Assert.Equal(SaveWorkLayout.TotalSize, coveredEnd);
        Assert.Equal(SaveWork.Size, SaveWorkLayout.TotalSize);
        Assert.Equal(0x9CDC, SaveWorkLayout.TotalSize);
    }
}

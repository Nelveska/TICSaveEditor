using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class FoundItemCollectionTests
{
    [Fact]
    public void Capacity_is_0x80()
    {
        Assert.Equal(0x80, FoundItemCollection.Capacity);
    }

    [Fact]
    public void AllSlots_count_equals_Capacity()
    {
        var found = new FoundItemCollection(new byte[FoundItemCollection.Capacity]);
        Assert.Equal(FoundItemCollection.Capacity, found.AllSlots.Count);
    }

    [Fact]
    public void Constructor_throws_on_wrong_length()
    {
        Assert.Throws<ArgumentException>(() => new FoundItemCollection(new byte[100]));
    }

    [Fact]
    public void GetCount_throws_on_out_of_range_index()
    {
        var found = new FoundItemCollection(new byte[FoundItemCollection.Capacity]);
        Assert.Throws<ArgumentOutOfRangeException>(() => found.GetCount(FoundItemCollection.Capacity));
    }

    [Fact]
    public void NonEmpty_live_updates()
    {
        var found = new FoundItemCollection(new byte[FoundItemCollection.Capacity]);
        Assert.Empty(found.NonEmpty);
        found.SetCount(0x10, 1);
        Assert.Single(found.NonEmpty);
        found.SetCount(0x10, 0);
        Assert.Empty(found.NonEmpty);
    }
}

using System.ComponentModel;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class InventoryEntryTests
{
    [Fact]
    public void StorageIndex_is_immutable()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.Equal(0, party.AllSlots[0].StorageIndex);
        Assert.Equal(5, party.AllSlots[5].StorageIndex);
        Assert.Equal(PartyInventory.Capacity - 1, party.AllSlots[PartyInventory.Capacity - 1].StorageIndex);
    }

    [Fact]
    public void Count_setter_routes_through_owner()
    {
        var bytes = new byte[PartyInventory.Capacity];
        var party = new PartyInventory(bytes);
        party.AllSlots[7].Count = 42;
        Assert.Equal(42, bytes[7]);
        Assert.Equal(42, party.GetCount(7));
    }

    [Fact]
    public void Count_setter_fires_PropertyChanged_on_entry()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        var entry = party.AllSlots[3];
        var fired = new List<string>();
        ((INotifyPropertyChanged)entry).PropertyChanged +=
            (_, e) => fired.Add(e.PropertyName ?? string.Empty);

        entry.Count = 10;

        Assert.Contains(nameof(InventoryEntry.Count), fired);
        Assert.Contains(nameof(InventoryEntry.IsEmpty), fired);
    }

    [Fact]
    public void IsEmpty_reflects_zero_count()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.True(party.AllSlots[0].IsEmpty);
        party.AllSlots[0].Count = 5;
        Assert.False(party.AllSlots[0].IsEmpty);
    }
}

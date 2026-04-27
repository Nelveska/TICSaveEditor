using System.ComponentModel;
using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class PartyInventoryTests
{
    [Fact]
    public void Capacity_is_0x105()
    {
        Assert.Equal(0x105, PartyInventory.Capacity);
    }

    [Fact]
    public void AllSlots_count_equals_Capacity()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.Equal(PartyInventory.Capacity, party.AllSlots.Count);
    }

    [Fact]
    public void AllSlots_returns_stable_references()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.Same(party.AllSlots[42], party.AllSlots[42]);
    }

    [Fact]
    public void Constructor_throws_on_wrong_byte_length()
    {
        Assert.Throws<ArgumentException>(() => new PartyInventory(new byte[100]));
        Assert.Throws<ArgumentException>(() => new PartyInventory(new byte[PartyInventory.Capacity + 1]));
    }

    [Fact]
    public void GetCount_throws_on_out_of_range_index()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.Throws<ArgumentOutOfRangeException>(() => party.GetCount(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => party.GetCount(PartyInventory.Capacity));
    }

    [Fact]
    public void SetCount_throws_on_out_of_range_index()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.Throws<ArgumentOutOfRangeException>(() => party.SetCount(-1, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => party.SetCount(PartyInventory.Capacity, 5));
    }

    [Fact]
    public void SetCount_clamps_to_byte_range()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        party.SetCount(0, 1000);
        Assert.Equal(255, party.GetCount(0));
        party.SetCount(0, -50);
        Assert.Equal(0, party.GetCount(0));
    }

    [Fact]
    public void NonEmpty_starts_empty_when_all_bytes_are_zero()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.Empty(party.NonEmpty);
    }

    [Fact]
    public void NonEmpty_includes_pre_populated_bytes_at_construction()
    {
        var bytes = new byte[PartyInventory.Capacity];
        bytes[5] = 3;
        bytes[100] = 1;
        var party = new PartyInventory(bytes);
        Assert.Equal(2, party.NonEmpty.Count);
        Assert.Equal(5, party.NonEmpty[0].StorageIndex);
        Assert.Equal(100, party.NonEmpty[1].StorageIndex);
    }

    [Fact]
    public void NonEmpty_adds_entry_when_count_crosses_zero_to_nonzero()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        Assert.Empty(party.NonEmpty);
        party.SetCount(7, 5);
        Assert.Single(party.NonEmpty);
        Assert.Equal(7, party.NonEmpty[0].StorageIndex);
    }

    [Fact]
    public void NonEmpty_removes_entry_when_count_crosses_nonzero_to_zero()
    {
        var bytes = new byte[PartyInventory.Capacity];
        bytes[10] = 4;
        var party = new PartyInventory(bytes);
        Assert.Single(party.NonEmpty);
        party.SetCount(10, 0);
        Assert.Empty(party.NonEmpty);
    }

    [Fact]
    public void NonEmpty_stays_ordered_by_StorageIndex_ascending()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        party.SetCount(50, 1);
        party.SetCount(10, 1);
        party.SetCount(200, 1);
        party.SetCount(30, 1);
        Assert.Equal(new[] { 10, 30, 50, 200 },
            party.NonEmpty.Select(e => e.StorageIndex).ToArray());
    }

    [Fact]
    public void NonEmpty_event_fires_on_zero_to_nonzero_transition()
    {
        var party = new PartyInventory(new byte[PartyInventory.Capacity]);
        var fired = new List<string>();
        ((INotifyPropertyChanged)party).PropertyChanged +=
            (_, e) => fired.Add(e.PropertyName ?? string.Empty);

        party.SetCount(5, 3);
        Assert.Contains(nameof(PartyInventory.NonEmpty), fired);
    }

    [Fact]
    public void NonEmpty_event_does_not_fire_when_count_changes_within_nonzero_range()
    {
        var bytes = new byte[PartyInventory.Capacity];
        bytes[5] = 3;
        var party = new PartyInventory(bytes);
        var fired = new List<string>();
        ((INotifyPropertyChanged)party).PropertyChanged +=
            (_, e) => fired.Add(e.PropertyName ?? string.Empty);

        party.SetCount(5, 7);  // 3 → 7, both non-zero
        Assert.DoesNotContain(nameof(PartyInventory.NonEmpty), fired);
    }

    [Fact]
    public void Real_fixture_BuyDagger_has_count_1_at_storage_index_0x01()
    {
        // M9 Phase 1 verification: BuyDagger vs Baseline2 confirmed PartyItemRaw[0x01] += 1.
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SaveFiles", "BuyDagger", "enhanced.png");
        if (!File.Exists(path)) return;

        var bytes = File.ReadAllBytes(path);
        var save = (TICSaveEditor.Core.Save.ManualSaveFile)
            TICSaveEditor.Core.Save.SaveFileLoader.Load(bytes, path);
        // Find most-recent populated slot (= the one with the buy event).
        var slot = save.Slots
            .Where(s => !s.IsEmpty)
            .OrderByDescending(s => s.SaveTimestamp)
            .First();
        Assert.True(slot.SaveWork.Battle.PartyInventory.GetCount(0x01) >= 1,
            $"Expected ≥1 at storage 0x01 in BuyDagger; got {slot.SaveWork.Battle.PartyInventory.GetCount(0x01)}.");
    }
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records;

/// <summary>
/// Wrapper over the BattleSection ShopItem byte region. Same shape as
/// <see cref="PartyInventory"/> at the same capacity (0x105). Tracks shop-availability
/// item counts. Per decisions_m9_partyitem_size_confirmed.md, the mirror-at-0x105
/// assumption is unverified by SaveDiff — same risk profile as M6 PartyItemRaw vs ShopItemRaw.
/// </summary>
public class ShopInventory : INotifyPropertyChanged, IInventoryEntryOwner
{
    public const int Capacity = 0x105;

    private readonly byte[] _bytes;
    private readonly InventoryEntry[] _entries;
    private readonly ObservableCollection<InventoryEntry> _nonEmpty;

    internal ShopInventory(byte[] bytes)
    {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != Capacity)
            throw new ArgumentException(
                $"ShopInventory requires exactly {Capacity} bytes (got {bytes.Length}).",
                nameof(bytes));

        _bytes = bytes;
        _entries = new InventoryEntry[Capacity];
        for (int i = 0; i < Capacity; i++)
            _entries[i] = new InventoryEntry(this, i);

        AllSlots = new ReadOnlyObservableCollection<InventoryEntry>(
            new ObservableCollection<InventoryEntry>(_entries));

        _nonEmpty = new ObservableCollection<InventoryEntry>();
        for (int i = 0; i < Capacity; i++)
            if (_bytes[i] != 0) _nonEmpty.Add(_entries[i]);

        NonEmpty = new ReadOnlyObservableCollection<InventoryEntry>(_nonEmpty);
    }

    public ReadOnlyObservableCollection<InventoryEntry> AllSlots { get; }
    public ReadOnlyObservableCollection<InventoryEntry> NonEmpty { get; }

    public int GetCount(int storageIndex)
    {
        if ((uint)storageIndex >= (uint)Capacity)
            throw new ArgumentOutOfRangeException(nameof(storageIndex));
        return _bytes[storageIndex];
    }

    public void SetCount(int storageIndex, int count)
    {
        if ((uint)storageIndex >= (uint)Capacity)
            throw new ArgumentOutOfRangeException(nameof(storageIndex));

        var clamped = (byte)Math.Clamp(count, 0, 255);
        var previous = _bytes[storageIndex];
        if (clamped == previous) return;

        _bytes[storageIndex] = clamped;
        _entries[storageIndex].RaiseCountChanged();

        bool wasEmpty = previous == 0;
        bool nowEmpty = clamped == 0;
        if (wasEmpty && !nowEmpty)
        {
            InsertSorted(_entries[storageIndex]);
            OnPropertyChanged(nameof(NonEmpty));
        }
        else if (!wasEmpty && nowEmpty)
        {
            _nonEmpty.Remove(_entries[storageIndex]);
            OnPropertyChanged(nameof(NonEmpty));
        }
    }

    private void InsertSorted(InventoryEntry entry)
    {
        int lo = 0, hi = _nonEmpty.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (_nonEmpty[mid].StorageIndex < entry.StorageIndex) lo = mid + 1;
            else hi = mid;
        }
        _nonEmpty.Insert(lo, entry);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records;

/// <summary>
/// Wrapper over the BattleSection PartyItem byte region. Each byte in the underlying
/// 261-byte array stores the count of one item at that storage index. Storage indices
/// are category-grouped positions (see decisions_m9_wrapper_api_storage_index.md), NOT
/// game item IDs — bidirectional name lookup is a viewmodel-layer concern.
/// </summary>
public class PartyInventory : INotifyPropertyChanged, IInventoryEntryOwner
{
    public const int Capacity = 0x105;

    private readonly byte[] _bytes;
    private readonly InventoryEntry[] _entries;
    private readonly ObservableCollection<InventoryEntry> _nonEmpty;

    internal PartyInventory(byte[] bytes)
    {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != Capacity)
            throw new ArgumentException(
                $"PartyInventory requires exactly {Capacity} bytes (got {bytes.Length}).",
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

    /// <summary>Stable, full-capacity view (always 261 entries; references never change).</summary>
    public ReadOnlyObservableCollection<InventoryEntry> AllSlots { get; }

    /// <summary>
    /// Live-filtered view of entries with non-zero <see cref="InventoryEntry.Count"/>,
    /// ordered by <see cref="InventoryEntry.StorageIndex"/> ascending. Entries are inserted
    /// when count crosses 0 → non-zero and removed when crossing non-zero → 0.
    /// </summary>
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

        // Clamp to byte range; out-of-range is silently clamped for ergonomic edits.
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
        // Binary search by StorageIndex to keep NonEmpty ordered.
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

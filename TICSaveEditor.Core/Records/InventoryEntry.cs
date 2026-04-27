using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records;

/// <summary>
/// One entry in an inventory wrapper (PartyInventory / ShopInventory / FoundItemCollection).
/// <see cref="StorageIndex"/> is the byte position in the underlying region (NOT a game item ID;
/// see decisions_m9_wrapper_api_storage_index.md). Setting <see cref="Count"/> writes through
/// to the owner, which clamps to byte range [0, 255] and updates the wrapper's NonEmpty filter.
/// </summary>
public class InventoryEntry : INotifyPropertyChanged
{
    private readonly IInventoryEntryOwner _owner;

    internal InventoryEntry(IInventoryEntryOwner owner, int storageIndex)
    {
        _owner = owner;
        StorageIndex = storageIndex;
    }

    public int StorageIndex { get; }

    public int Count
    {
        get => _owner.GetCount(StorageIndex);
        set => _owner.SetCount(StorageIndex, value);
    }

    public bool IsEmpty => Count == 0;

    internal void RaiseCountChanged()
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(IsEmpty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Internal contract that lets <see cref="InventoryEntry"/> route Count reads/writes
/// through whichever inventory wrapper owns it.
/// </summary>
internal interface IInventoryEntryOwner
{
    int GetCount(int storageIndex);
    void SetCount(int storageIndex, int count);
}

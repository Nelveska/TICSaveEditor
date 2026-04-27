using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records.Entries;

public class EquipItemEntry : INotifyPropertyChanged, IRaisableEntry
{
    private readonly UnitSaveData _owner;

    internal EquipItemEntry(UnitSaveData owner, int index)
    {
        _owner = owner;
        Index = index;
    }

    public int Index { get; }

    public EquipmentSlot Slot => (EquipmentSlot)Index;

    public ushort Value
    {
        get => _owner.GetEquipItem(Index);
        set => _owner.SetEquipItem(Index, value);
    }

    public bool IsEmpty => Value == UnitSaveData.EmptyEquipSlotSentinel;

    void IRaisableEntry.RaiseValueChanged()
    {
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(IsEmpty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

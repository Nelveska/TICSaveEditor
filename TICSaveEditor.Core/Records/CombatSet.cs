using System.ComponentModel;
using System.Runtime.CompilerServices;
using TICSaveEditor.Core.Records.Entries;

namespace TICSaveEditor.Core.Records;

public class CombatSet : INotifyPropertyChanged, IRaisableEntry
{
    public const int NameByteLength = 66;
    public const int ItemBytesLength = 10;
    public const int AbilityBytesLength = 10;

    private readonly UnitSaveData _owner;

    internal CombatSet(UnitSaveData owner, int index)
    {
        _owner = owner;
        Index = index;
    }

    public int Index { get; }

    public string Name
    {
        get => _owner.GetCombatSetName(Index);
        set => _owner.SetCombatSetName(Index, value);
    }

    public byte Job
    {
        get => _owner.GetCombatSetJob(Index);
        set => _owner.SetCombatSetJob(Index, value);
    }

    public bool IsDoubleHand
    {
        get => _owner.GetCombatSetIsDoubleHand(Index);
        set => _owner.SetCombatSetIsDoubleHand(Index, value);
    }

    /// <summary>
    /// 10 raw bytes assumed to hold 5 × u16 item IDs at section-relative offset 0x42..0x4B.
    /// The boundary at offset 0x4C between Items and Abilities is the spec assumption — it
    /// is NOT empirically verified by SaveDiff (the M5.5 CombatSet fixture changed both regions
    /// simultaneously). If a future SaveDiff with isolated item-vs-ability edits shifts the
    /// boundary, this property re-slices internally; the public API does not change.
    /// See decisions_equipset_layout_resolved.md.
    /// </summary>
    public byte[] RawItemBytes => _owner.GetCombatSetItemBytes(Index);

    /// <summary>
    /// 10 raw bytes assumed to hold 5 × u16 ability IDs at section-relative offset 0x4C..0x55.
    /// Boundary unverified — see RawItemBytes for details.
    /// </summary>
    public byte[] RawAbilityBytes => _owner.GetCombatSetAbilityBytes(Index);

    void IRaisableEntry.RaiseValueChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Job));
        OnPropertyChanged(nameof(IsDoubleHand));
        OnPropertyChanged(nameof(RawItemBytes));
        OnPropertyChanged(nameof(RawAbilityBytes));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

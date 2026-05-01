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
    /// 10 raw bytes holding 5 × u16 item IDs at section-relative offset 0x42..0x4B,
    /// confirmed empirically by the 2026-05-01 ChangeOneItem SaveDiff fixture (Accessory
    /// edit landed at CS0+0x4A). Item slot order per the community TIC struct: RHWeapon
    /// (0x42), LHShield (0x44), Head (0x46), Armor (0x48), Accessory (0x4A). A future
    /// Core API rename may decompose this into named u16 fields; today the byte[] form
    /// preserves byte-faithfulness pending that work. See
    /// decisions_equipset_layout_resolved.md for the resolution + fixture trail.
    /// </summary>
    public byte[] RawItemBytes => _owner.GetCombatSetItemBytes(Index);

    /// <summary>
    /// 10 raw bytes spanning section-relative offset 0x4C..0x55. Empirical decomposition
    /// (community TIC struct, confirmed 2026-05-01 by the ChangeOneAbilitySlot +
    /// ChangeOneSkillset fixtures): 0x4C..0x4F = 2 × i16 skillsets (NOT u16 abilities as
    /// the spec/Nenkai claimed), 0x50..0x55 = 3 × u16 abilities (Reaction at 0x50, Support
    /// at 0x52, Movement at 0x54). The "AbilityBytes" name is preserved for API stability;
    /// the named decomposition is queued for a future Core API rename. See
    /// decisions_equipset_layout_resolved.md.
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

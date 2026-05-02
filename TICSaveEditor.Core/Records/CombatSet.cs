using System.ComponentModel;
using System.Runtime.CompilerServices;
using TICSaveEditor.Core.Records.Entries;

namespace TICSaveEditor.Core.Records;

public class CombatSet : INotifyPropertyChanged, IRaisableEntry
{
    public const int NameByteLength = 16;
    public const int NamePaddingByteLength = 50;
    public const int SkillsetsByteLength = 4;
    public const int AbilitiesByteLength = 6;
    public const int SkillsetCount = 2;
    public const int AbilityCount = 3;
    public const int ItemSlotCount = 5;

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

    /// <summary>i16 skillset slot 0 at section-relative 0x4C..0x4D.</summary>
    public short Skillset0
    {
        get => _owner.GetCombatSetSkillset(Index, 0);
        set => _owner.SetCombatSetSkillset(Index, 0, value);
    }

    /// <summary>i16 skillset slot 1 at section-relative 0x4E..0x4F.</summary>
    public short Skillset1
    {
        get => _owner.GetCombatSetSkillset(Index, 1);
        set => _owner.SetCombatSetSkillset(Index, 1, value);
    }

    /// <summary>u16 reaction-ability id at section-relative 0x50..0x51.</summary>
    public ushort ReactionAbility
    {
        get => _owner.GetCombatSetAbility(Index, 0);
        set => _owner.SetCombatSetAbility(Index, 0, value);
    }

    /// <summary>u16 support-ability id at section-relative 0x52..0x53.</summary>
    public ushort SupportAbility
    {
        get => _owner.GetCombatSetAbility(Index, 1);
        set => _owner.SetCombatSetAbility(Index, 1, value);
    }

    /// <summary>u16 movement-ability id at section-relative 0x54..0x55.</summary>
    public ushort MovementAbility
    {
        get => _owner.GetCombatSetAbility(Index, 2);
        set => _owner.SetCombatSetAbility(Index, 2, value);
    }

    /// <summary>u16 right-hand item id at section-relative 0x42..0x43. Empty = <see cref="UnitSaveData.EmptyEquipSlotSentinel"/>.</summary>
    public ushort Rh
    {
        get => _owner.GetCombatSetItem(Index, 0);
        set => _owner.SetCombatSetItem(Index, 0, value);
    }

    /// <summary>u16 left-hand item id at section-relative 0x44..0x45. Empty = <see cref="UnitSaveData.EmptyEquipSlotSentinel"/>.</summary>
    public ushort Lh
    {
        get => _owner.GetCombatSetItem(Index, 1);
        set => _owner.SetCombatSetItem(Index, 1, value);
    }

    /// <summary>u16 head-slot item id at section-relative 0x46..0x47. Empty = <see cref="UnitSaveData.EmptyEquipSlotSentinel"/>.</summary>
    public ushort Head
    {
        get => _owner.GetCombatSetItem(Index, 2);
        set => _owner.SetCombatSetItem(Index, 2, value);
    }

    /// <summary>u16 body-armor item id at section-relative 0x48..0x49. Empty = <see cref="UnitSaveData.EmptyEquipSlotSentinel"/>.</summary>
    public ushort Armor
    {
        get => _owner.GetCombatSetItem(Index, 3);
        set => _owner.SetCombatSetItem(Index, 3, value);
    }

    /// <summary>u16 accessory item id at section-relative 0x4A..0x4B. Empty = <see cref="UnitSaveData.EmptyEquipSlotSentinel"/>.</summary>
    public ushort Accessory
    {
        get => _owner.GetCombatSetItem(Index, 4);
        set => _owner.SetCombatSetItem(Index, 4, value);
    }

    void IRaisableEntry.RaiseValueChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Job));
        OnPropertyChanged(nameof(IsDoubleHand));
        OnPropertyChanged(nameof(Skillset0));
        OnPropertyChanged(nameof(Skillset1));
        OnPropertyChanged(nameof(ReactionAbility));
        OnPropertyChanged(nameof(SupportAbility));
        OnPropertyChanged(nameof(MovementAbility));
        OnPropertyChanged(nameof(Rh));
        OnPropertyChanged(nameof(Lh));
        OnPropertyChanged(nameof(Head));
        OnPropertyChanged(nameof(Armor));
        OnPropertyChanged(nameof(Accessory));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

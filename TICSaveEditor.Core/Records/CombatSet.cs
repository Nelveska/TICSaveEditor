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
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TICSaveEditor.Core.Records.Entries;

public class JobAbilityFlagsEntry : INotifyPropertyChanged, IRaisableEntry
{
    public const int BitCount = 24;
    public const int ActiveBitCount = 16;
    public const int PassiveBitCount = 8;

    private readonly UnitSaveData _owner;

    internal JobAbilityFlagsEntry(UnitSaveData owner, int jobId)
    {
        _owner = owner;
        JobId = jobId;
    }

    public int JobId { get; }

    public byte[] RawBytes
    {
        get
        {
            var copy = new byte[3];
            copy[0] = _owner.GetAbilityFlagByte(JobId, 0);
            copy[1] = _owner.GetAbilityFlagByte(JobId, 1);
            copy[2] = _owner.GetAbilityFlagByte(JobId, 2);
            return copy;
        }
    }

    public bool AllLearned
        => _owner.GetAbilityFlagByte(JobId, 0) == 0xFF
            && _owner.GetAbilityFlagByte(JobId, 1) == 0xFF
            && _owner.GetAbilityFlagByte(JobId, 2) == 0xFF;

    public bool NoneLearned
        => _owner.GetAbilityFlagByte(JobId, 0) == 0x00
            && _owner.GetAbilityFlagByte(JobId, 1) == 0x00
            && _owner.GetAbilityFlagByte(JobId, 2) == 0x00;

    public bool IsLearned(int abilityIndex)
    {
        if ((uint)abilityIndex >= (uint)BitCount)
            throw new ArgumentOutOfRangeException(nameof(abilityIndex));

        var byteIndex = abilityIndex >> 3;
        var bitIndex = abilityIndex & 0b111;
        var b = _owner.GetAbilityFlagByte(JobId, byteIndex);
        return (b & (1 << bitIndex)) != 0;
    }

    public void SetLearned(int abilityIndex, bool value)
    {
        if ((uint)abilityIndex >= (uint)BitCount)
            throw new ArgumentOutOfRangeException(nameof(abilityIndex));

        var byteIndex = abilityIndex >> 3;
        var bitIndex = abilityIndex & 0b111;
        var current = _owner.GetAbilityFlagByte(JobId, byteIndex);
        byte updated = value
            ? (byte)(current | (1 << bitIndex))
            : (byte)(current & ~(1 << bitIndex));
        if (updated == current) return;
        _owner.SetAbilityFlagByte(JobId, byteIndex, updated);
    }

    public bool IsActiveLearned(int activeIndex)
    {
        if ((uint)activeIndex >= (uint)ActiveBitCount)
            throw new ArgumentOutOfRangeException(nameof(activeIndex));
        return IsLearned(activeIndex);
    }

    public void SetActiveLearned(int activeIndex, bool value)
    {
        if ((uint)activeIndex >= (uint)ActiveBitCount)
            throw new ArgumentOutOfRangeException(nameof(activeIndex));
        SetLearned(activeIndex, value);
    }

    public bool IsPassiveLearned(int passiveIndex)
    {
        if ((uint)passiveIndex >= (uint)PassiveBitCount)
            throw new ArgumentOutOfRangeException(nameof(passiveIndex));
        return IsLearned(ActiveBitCount + passiveIndex);
    }

    public void SetPassiveLearned(int passiveIndex, bool value)
    {
        if ((uint)passiveIndex >= (uint)PassiveBitCount)
            throw new ArgumentOutOfRangeException(nameof(passiveIndex));
        SetLearned(ActiveBitCount + passiveIndex, value);
    }

    public void LearnAll()
    {
        _owner.SetAbilityFlagByte(JobId, 0, 0xFF);
        _owner.SetAbilityFlagByte(JobId, 1, 0xFF);
        _owner.SetAbilityFlagByte(JobId, 2, 0xFF);
    }

    public void ForgetAll()
    {
        _owner.SetAbilityFlagByte(JobId, 0, 0x00);
        _owner.SetAbilityFlagByte(JobId, 1, 0x00);
        _owner.SetAbilityFlagByte(JobId, 2, 0x00);
    }

    void IRaisableEntry.RaiseValueChanged()
    {
        // Bit/byte-level updates fan out to multiple computed properties; null name = all changed.
        OnPropertyChanged(string.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

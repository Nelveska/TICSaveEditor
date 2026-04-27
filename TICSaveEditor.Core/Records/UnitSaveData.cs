using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TICSaveEditor.Core.Records.Entries;
using TICSaveEditor.Core.Records.Layouts;
using TICSaveEditor.Core.Validation;

namespace TICSaveEditor.Core.Records;

public class UnitSaveData : INotifyPropertyChanged
{
    public const int Size = 600;
    public const ushort EmptyEquipSlotSentinel = 0x00FF;

    private const int EquipItemCount = 7;
    private const int JobLevelCount = 12;
    private const int JobPointCount = 23;
    private const int AbilityFlagJobCount = 22;
    private const int AbilityFlagBytesPerJob = 3;
    private const int ChrNameLength = 64;
    private const int MaxStatBase = 0xFFFFFF;
    private const int EquipSetCount = 3;

    private UnitSaveDataLayout _layout;

    private readonly EquipItemEntry[] _equipItemEntries;
    private readonly JobLevelEntry[] _jobLevelEntries;
    private readonly JobPointEntry[] _jobPointEntries;
    private readonly TotalJobPointEntry[] _totalJobPointEntries;
    private readonly JobAbilityFlagsEntry[] _abilityFlagsEntries;
    private readonly EquipSet[] _equipSetEntries;

    private int _suspendDepth;
    private readonly HashSet<string> _suspendedCollections = new();

    internal UnitSaveData(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException(
                $"UnitSaveData requires exactly {Size} bytes (got {bytes.Length}).",
                nameof(bytes));
        }

        _layout = MemoryMarshal.Read<UnitSaveDataLayout>(bytes);

        _equipItemEntries = new EquipItemEntry[EquipItemCount];
        for (int i = 0; i < EquipItemCount; i++) _equipItemEntries[i] = new EquipItemEntry(this, i);
        EquipItems = new ReadOnlyObservableCollection<EquipItemEntry>(
            new ObservableCollection<EquipItemEntry>(_equipItemEntries));

        _jobLevelEntries = new JobLevelEntry[JobLevelCount];
        for (int i = 0; i < JobLevelCount; i++) _jobLevelEntries[i] = new JobLevelEntry(this, i);
        JobLevels = new ReadOnlyObservableCollection<JobLevelEntry>(
            new ObservableCollection<JobLevelEntry>(_jobLevelEntries));

        _jobPointEntries = new JobPointEntry[JobPointCount];
        for (int i = 0; i < JobPointCount; i++) _jobPointEntries[i] = new JobPointEntry(this, i);
        JobPoints = new ReadOnlyObservableCollection<JobPointEntry>(
            new ObservableCollection<JobPointEntry>(_jobPointEntries));

        _totalJobPointEntries = new TotalJobPointEntry[JobPointCount];
        for (int i = 0; i < JobPointCount; i++) _totalJobPointEntries[i] = new TotalJobPointEntry(this, i);
        TotalJobPoints = new ReadOnlyObservableCollection<TotalJobPointEntry>(
            new ObservableCollection<TotalJobPointEntry>(_totalJobPointEntries));

        _abilityFlagsEntries = new JobAbilityFlagsEntry[AbilityFlagJobCount];
        for (int i = 0; i < AbilityFlagJobCount; i++) _abilityFlagsEntries[i] = new JobAbilityFlagsEntry(this, i);
        AbilityFlags = new ReadOnlyObservableCollection<JobAbilityFlagsEntry>(
            new ObservableCollection<JobAbilityFlagsEntry>(_abilityFlagsEntries));

        _equipSetEntries = new EquipSet[EquipSetCount];
        for (int i = 0; i < EquipSetCount; i++) _equipSetEntries[i] = new EquipSet(this, i);
        EquipSets = new ReadOnlyObservableCollection<EquipSet>(
            new ObservableCollection<EquipSet>(_equipSetEntries));
    }

    internal void WriteTo(Span<byte> destination)
    {
        if (destination.Length != Size)
        {
            throw new ArgumentException(
                $"UnitSaveData.WriteTo requires exactly {Size} bytes (got {destination.Length}).",
                nameof(destination));
        }

        MemoryMarshal.Write(destination, in _layout);
    }

    /// <summary>
    /// Replaces the unit's <c>_layout</c> from a snapshot byte slice and fires a single
    /// <c>OnPropertyChanged(null)</c>. Existing entry collection references (EquipItems,
    /// JobLevels, etc.) remain valid — their getters re-read through the new <c>_layout</c>.
    /// Called during M8 SaveWork rollback.
    /// </summary>
    internal void RehydrateFrom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException(
                $"UnitSaveData.RehydrateFrom requires exactly {Size} bytes (got {bytes.Length}).",
                nameof(bytes));
        }
        _layout = MemoryMarshal.Read<UnitSaveDataLayout>(bytes);
        OnPropertyChanged(null);
    }

    // ===== Identity (0x00..0x0D) =====

    public byte Character
    {
        get => _layout.Character;
        set { if (_layout.Character == value) return; _layout.Character = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEmpty)); }
    }

    public byte Resist
    {
        get => _layout.Resist;
        set { if (_layout.Resist == value) return; _layout.Resist = value; OnPropertyChanged(); }
    }

    public byte Job
    {
        get => _layout.Job;
        set { if (_layout.Job == value) return; _layout.Job = value; OnPropertyChanged(); }
    }

    public byte Union
    {
        get => _layout.Union;
        set { if (_layout.Union == value) return; _layout.Union = value; OnPropertyChanged(); }
    }

    public byte Sex
    {
        get => _layout.Sex;
        set { if (_layout.Sex == value) return; _layout.Sex = value; OnPropertyChanged(); }
    }

    public byte Reserved05
    {
        get => _layout.Reserved05;
        set { if (_layout.Reserved05 == value) return; _layout.Reserved05 = value; OnPropertyChanged(); }
    }

    public byte Zodiac
    {
        get => _layout.Zodiac;
        set { if (_layout.Zodiac == value) return; _layout.Zodiac = value; OnPropertyChanged(); }
    }

    public byte SubCommand
    {
        get => _layout.SubCommand;
        set { if (_layout.SubCommand == value) return; _layout.SubCommand = value; OnPropertyChanged(); }
    }

    public ushort ReactionAbility
    {
        get => _layout.ReactionAbility;
        set { if (_layout.ReactionAbility == value) return; _layout.ReactionAbility = value; OnPropertyChanged(); }
    }

    public ushort SupportAbility
    {
        get => _layout.SupportAbility;
        set { if (_layout.SupportAbility == value) return; _layout.SupportAbility = value; OnPropertyChanged(); }
    }

    public ushort MoveAbility
    {
        get => _layout.MoveAbility;
        set { if (_layout.MoveAbility == value) return; _layout.MoveAbility = value; OnPropertyChanged(); }
    }

    // ===== Equipment (0x0E..0x1B) =====

    public ReadOnlyObservableCollection<EquipItemEntry> EquipItems { get; }

    public ushort GetEquipItem(int slotIndex)
    {
        if ((uint)slotIndex >= (uint)EquipItemCount)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        unsafe { return _layout.EquipItem[slotIndex]; }
    }

    public void SetEquipItem(int slotIndex, ushort itemId)
    {
        if ((uint)slotIndex >= (uint)EquipItemCount)
            throw new ArgumentOutOfRangeException(nameof(slotIndex));

        unsafe
        {
            if (_layout.EquipItem[slotIndex] == itemId) return;
            _layout.EquipItem[slotIndex] = itemId;
        }
        NotifyOrQueue(nameof(EquipItems), _equipItemEntries[slotIndex]);
    }

    // ===== Progression (0x1C..0x1F) =====

    public byte Exp
    {
        get => _layout.Exp;
        set { if (_layout.Exp == value) return; _layout.Exp = value; OnPropertyChanged(); }
    }

    public byte Level
    {
        get => _layout.Level;
        set { if (_layout.Level == value) return; _layout.Level = value; OnPropertyChanged(); }
    }

    public byte StartBcp
    {
        get => _layout.StartBcp;
        set { if (_layout.StartBcp == value) return; _layout.StartBcp = value; OnPropertyChanged(); }
    }

    public byte StartFaith
    {
        get => _layout.StartFaith;
        set { if (_layout.StartFaith == value) return; _layout.StartFaith = value; OnPropertyChanged(); }
    }

    // ===== Base stats (24-bit LE; 0x20..0x31) =====

    public int HpMaxBase
    {
        get { unsafe { fixed (byte* p = _layout.HpMaxBase) return Read24(p); } }
        set { ValidateStatBase(value); unsafe { fixed (byte* p = _layout.HpMaxBase) Write24(p, value); } OnPropertyChanged(); }
    }

    public int MpMaxBase
    {
        get { unsafe { fixed (byte* p = _layout.MpMaxBase) return Read24(p); } }
        set { ValidateStatBase(value); unsafe { fixed (byte* p = _layout.MpMaxBase) Write24(p, value); } OnPropertyChanged(); }
    }

    public int WtBase
    {
        get { unsafe { fixed (byte* p = _layout.WtBase) return Read24(p); } }
        set { ValidateStatBase(value); unsafe { fixed (byte* p = _layout.WtBase) Write24(p, value); } OnPropertyChanged(); }
    }

    public int AtBase
    {
        get { unsafe { fixed (byte* p = _layout.AtBase) return Read24(p); } }
        set { ValidateStatBase(value); unsafe { fixed (byte* p = _layout.AtBase) Write24(p, value); } OnPropertyChanged(); }
    }

    public int MatBase
    {
        get { unsafe { fixed (byte* p = _layout.MatBase) return Read24(p); } }
        set { ValidateStatBase(value); unsafe { fixed (byte* p = _layout.MatBase) Write24(p, value); } OnPropertyChanged(); }
    }

    public int JobChangeFlag
    {
        get { unsafe { fixed (byte* p = _layout.JobChangeFlag) return Read24(p); } }
        set { ValidateStatBase(value); unsafe { fixed (byte* p = _layout.JobChangeFlag) Write24(p, value); } OnPropertyChanged(); }
    }

    private static void ValidateStatBase(int value, [CallerMemberName] string? name = null)
    {
        if ((uint)value > (uint)MaxStatBase)
            throw new ArgumentOutOfRangeException(name, value, $"24-bit base stat must be in [0, {MaxStatBase}].");
    }

    private static unsafe int Read24(byte* p) => p[0] | (p[1] << 8) | (p[2] << 16);

    private static unsafe void Write24(byte* p, int value)
    {
        p[0] = (byte)value;
        p[1] = (byte)(value >> 8);
        p[2] = (byte)(value >> 16);
    }

    // ===== Job levels (0x74..0x7F; 12 × u8) =====

    public ReadOnlyObservableCollection<JobLevelEntry> JobLevels { get; }

    public byte GetJobLevel(int jobId)
    {
        if ((uint)jobId >= (uint)JobLevelCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        unsafe { return _layout.JobLevel[jobId]; }
    }

    public void SetJobLevel(int jobId, byte level)
    {
        if ((uint)jobId >= (uint)JobLevelCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        unsafe
        {
            if (_layout.JobLevel[jobId] == level) return;
            _layout.JobLevel[jobId] = level;
        }
        NotifyOrQueue(nameof(JobLevels), _jobLevelEntries[jobId]);
    }

    // ===== Job points (0x80..0xAD; 23 × u16) =====

    public ReadOnlyObservableCollection<JobPointEntry> JobPoints { get; }

    public ushort GetJobPoint(int jobId)
    {
        if ((uint)jobId >= (uint)JobPointCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        unsafe { return _layout.JobPoint[jobId]; }
    }

    public void SetJobPoint(int jobId, ushort value)
    {
        if ((uint)jobId >= (uint)JobPointCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        unsafe
        {
            if (_layout.JobPoint[jobId] == value) return;
            _layout.JobPoint[jobId] = value;
        }
        NotifyOrQueue(nameof(JobPoints), _jobPointEntries[jobId]);
    }

    // ===== Total job points (0xAE..0xDB; 23 × u16) =====

    public ReadOnlyObservableCollection<TotalJobPointEntry> TotalJobPoints { get; }

    public ushort GetTotalJobPoint(int jobId)
    {
        if ((uint)jobId >= (uint)JobPointCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        unsafe { return _layout.TotalJobPoint[jobId]; }
    }

    public void SetTotalJobPoint(int jobId, ushort value)
    {
        if ((uint)jobId >= (uint)JobPointCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        unsafe
        {
            if (_layout.TotalJobPoint[jobId] == value) return;
            _layout.TotalJobPoint[jobId] = value;
        }
        NotifyOrQueue(nameof(TotalJobPoints), _totalJobPointEntries[jobId]);
    }

    // ===== Ability flags (0x32..0x73; 22 × 3 bytes) =====

    public ReadOnlyObservableCollection<JobAbilityFlagsEntry> AbilityFlags { get; }

    internal byte GetAbilityFlagByte(int jobId, int byteIndex)
    {
        if ((uint)jobId >= (uint)AbilityFlagJobCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));
        if ((uint)byteIndex >= (uint)AbilityFlagBytesPerJob)
            throw new ArgumentOutOfRangeException(nameof(byteIndex));

        unsafe { return _layout.AbilityFlag[jobId * AbilityFlagBytesPerJob + byteIndex]; }
    }

    internal void SetAbilityFlagByte(int jobId, int byteIndex, byte value)
    {
        if ((uint)jobId >= (uint)AbilityFlagJobCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));
        if ((uint)byteIndex >= (uint)AbilityFlagBytesPerJob)
            throw new ArgumentOutOfRangeException(nameof(byteIndex));

        unsafe
        {
            int offset = jobId * AbilityFlagBytesPerJob + byteIndex;
            if (_layout.AbilityFlag[offset] == value) return;
            _layout.AbilityFlag[offset] = value;
        }
        NotifyOrQueue(nameof(AbilityFlags), _abilityFlagsEntries[jobId]);
    }

    // ===== Name + slot metadata (0xDC..0x125) =====

    public byte[] ChrNameRaw
    {
        get
        {
            var copy = new byte[ChrNameLength];
            unsafe
            {
                fixed (byte* p = _layout.ChrName)
                {
                    new ReadOnlySpan<byte>(p, ChrNameLength).CopyTo(copy);
                }
            }
            return copy;
        }
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (value.Length != ChrNameLength)
                throw new ArgumentException(
                    $"ChrNameRaw must be exactly {ChrNameLength} bytes (got {value.Length}).",
                    nameof(value));

            unsafe
            {
                fixed (byte* p = _layout.ChrName)
                {
                    value.AsSpan().CopyTo(new Span<byte>(p, ChrNameLength));
                }
            }
            OnPropertyChanged();
        }
    }

    public ushort NameNo
    {
        get => _layout.NameNo;
        set { if (_layout.NameNo == value) return; _layout.NameNo = value; OnPropertyChanged(); }
    }

    public byte InTrip
    {
        get => _layout.InTrip;
        set { if (_layout.InTrip == value) return; _layout.InTrip = value; OnPropertyChanged(); }
    }

    public byte Parasite
    {
        get => _layout.Parasite;
        set { if (_layout.Parasite == value) return; _layout.Parasite = value; OnPropertyChanged(); }
    }

    public byte EggColor
    {
        get => _layout.EggColor;
        set { if (_layout.EggColor == value) return; _layout.EggColor = value; OnPropertyChanged(); }
    }

    public byte PspKilledNum
    {
        get => _layout.PspKilledNum;
        set { if (_layout.PspKilledNum == value) return; _layout.PspKilledNum = value; OnPropertyChanged(); }
    }

    public byte UnitOrderId
    {
        get => _layout.UnitOrderId;
        set { if (_layout.UnitOrderId == value) return; _layout.UnitOrderId = value; OnPropertyChanged(); }
    }

    public byte UnitStartingTeam
    {
        get => _layout.UnitStartingTeam;
        set { if (_layout.UnitStartingTeam == value) return; _layout.UnitStartingTeam = value; OnPropertyChanged(); }
    }

    public byte UnitJoinId
    {
        get => _layout.UnitJoinId;
        set { if (_layout.UnitJoinId == value) return; _layout.UnitJoinId = value; OnPropertyChanged(); }
    }

    public byte CurrentEquipSetNumber
    {
        get => _layout.CurrentEquipSetNumber;
        set { if (_layout.CurrentEquipSetNumber == value) return; _layout.CurrentEquipSetNumber = value; OnPropertyChanged(); }
    }

    public ushort CharaNameKey
    {
        get => _layout.CharaNameKey;
        set { if (_layout.CharaNameKey == value) return; _layout.CharaNameKey = value; OnPropertyChanged(); }
    }

    // ===== EquipSets (3 × 88 bytes at 0x126..0x22D) =====

    public ReadOnlyObservableCollection<EquipSet> EquipSets { get; }

    private ref EquipSetLayout GetEquipSetRef(int index)
    {
        switch (index)
        {
            case 0: return ref _layout.EquipSet0;
            case 1: return ref _layout.EquipSet1;
            case 2: return ref _layout.EquipSet2;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal string GetEquipSetName(int index)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetEquipSetRef(index);
        unsafe
        {
            fixed (byte* p = slot.Name)
            {
                var span = new ReadOnlySpan<byte>(p, EquipSet.NameByteLength);
                var nullIdx = span.IndexOf((byte)0);
                var len = nullIdx < 0 ? EquipSet.NameByteLength : nullIdx;
                return Encoding.ASCII.GetString(span.Slice(0, len));
            }
        }
    }

    internal void SetEquipSetName(int index, string value)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetEquipSetRef(index);
        var v = value ?? string.Empty;
        var clampedLen = Math.Min(v.Length, EquipSet.NameByteLength - 1);

        unsafe
        {
            fixed (byte* p = slot.Name)
            {
                var dest = new Span<byte>(p, EquipSet.NameByteLength);
                var bytesWritten = Encoding.ASCII.GetBytes(v.AsSpan(0, clampedLen), dest);
                if (bytesWritten < EquipSet.NameByteLength)
                {
                    dest[bytesWritten] = 0;
                }
            }
        }
        NotifyOrQueue(nameof(EquipSets), _equipSetEntries[index]);
    }

    internal byte GetEquipSetJob(int index)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetEquipSetRef(index).Job;
    }

    internal void SetEquipSetJob(int index, byte value)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetEquipSetRef(index);
        if (slot.Job == value) return;
        slot.Job = value;
        NotifyOrQueue(nameof(EquipSets), _equipSetEntries[index]);
    }

    internal bool GetEquipSetIsDoubleHand(int index)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetEquipSetRef(index).IsDoubleHand != 0;
    }

    internal void SetEquipSetIsDoubleHand(int index, bool value)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetEquipSetRef(index);
        byte newByte = value ? (byte)1 : (byte)0;
        if (slot.IsDoubleHand == newByte) return;
        slot.IsDoubleHand = newByte;
        NotifyOrQueue(nameof(EquipSets), _equipSetEntries[index]);
    }

    internal byte[] GetEquipSetItemBytes(int index)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetEquipSetRef(index);
        var copy = new byte[EquipSet.ItemBytesLength];
        unsafe
        {
            fixed (byte* p = slot.ItemBytes)
            {
                new ReadOnlySpan<byte>(p, EquipSet.ItemBytesLength).CopyTo(copy);
            }
        }
        return copy;
    }

    internal byte[] GetEquipSetAbilityBytes(int index)
    {
        if ((uint)index >= (uint)EquipSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetEquipSetRef(index);
        var copy = new byte[EquipSet.AbilityBytesLength];
        unsafe
        {
            fixed (byte* p = slot.AbilityBytes)
            {
                new ReadOnlySpan<byte>(p, EquipSet.AbilityBytesLength).CopyTo(copy);
            }
        }
        return copy;
    }

    // ===== Empty detection =====

    public bool IsEmpty => _layout.Character == 0;

    // ===== Validation =====

    public ValidationResult Validate()
    {
        var issues = new List<ValidationIssue>();

        if (_layout.Level < 1 || _layout.Level > 99)
            issues.Add(new ValidationIssue(nameof(Level), $"Level must be in [1, 99] (got {_layout.Level})."));

        if (_layout.StartBcp > 100)
            issues.Add(new ValidationIssue(nameof(StartBcp), $"StartBcp must be ≤ 100 (got {_layout.StartBcp})."));

        if (_layout.StartFaith > 100)
            issues.Add(new ValidationIssue(nameof(StartFaith), $"StartFaith must be ≤ 100 (got {_layout.StartFaith})."));

        // Zodiac sign is encoded in the high nibble of byte 0x06; the low nibble is
        // a sub-flag (varies between 0x0 and 0x1 across observed real saves) and
        // must be preserved by editors. See decisions_glain_psx_formulas.md.
        var zodiacSign = (_layout.Zodiac & 0xF0) >> 4;
        if (zodiacSign > 11)
            issues.Add(new ValidationIssue(nameof(Zodiac),
                $"Zodiac sign must be in [0, 11] (got high-nibble {zodiacSign} from byte 0x{_layout.Zodiac:X2})."));

        if (_layout.CurrentEquipSetNumber > 2)
            issues.Add(new ValidationIssue(
                nameof(CurrentEquipSetNumber),
                $"CurrentEquipSetNumber must be in [0, 2] (got {_layout.CurrentEquipSetNumber})."));

        return issues.Count == 0
            ? ValidationResult.Empty
            : new ValidationResult(issues);
    }

    // ===== Single-unit bulk ops =====

    public void LearnAllAbilities()
    {
        using var _ = SuspendNotifications();
        for (int jobId = 0; jobId < AbilityFlagJobCount; jobId++)
        {
            for (int b = 0; b < AbilityFlagBytesPerJob; b++)
                SetAbilityFlagByte(jobId, b, 0xFF);
        }
    }

    public void ForgetAllAbilities()
    {
        using var _ = SuspendNotifications();
        for (int jobId = 0; jobId < AbilityFlagJobCount; jobId++)
        {
            for (int b = 0; b < AbilityFlagBytesPerJob; b++)
                SetAbilityFlagByte(jobId, b, 0x00);
        }
    }

    public void LearnAllAbilitiesForJob(int jobId)
    {
        if ((uint)jobId >= (uint)AbilityFlagJobCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        using var _ = SuspendNotifications();
        for (int b = 0; b < AbilityFlagBytesPerJob; b++)
            SetAbilityFlagByte(jobId, b, 0xFF);
    }

    public void ForgetAllAbilitiesForJob(int jobId)
    {
        if ((uint)jobId >= (uint)AbilityFlagJobCount)
            throw new ArgumentOutOfRangeException(nameof(jobId));

        using var _ = SuspendNotifications();
        for (int b = 0; b < AbilityFlagBytesPerJob; b++)
            SetAbilityFlagByte(jobId, b, 0x00);
    }

    public void MaxAllJobPoints()
    {
        using var _ = SuspendNotifications();
        for (int i = 0; i < JobPointCount; i++) SetJobPoint(i, ushort.MaxValue);
    }

    public void ZeroAllJobPoints()
    {
        using var _ = SuspendNotifications();
        for (int i = 0; i < JobPointCount; i++) SetJobPoint(i, 0);
    }

    // ===== Suspend scope (private; M7 ISuspendable is a separate concern) =====

    private IDisposable SuspendNotifications()
    {
        _suspendDepth++;
        return new SuspendScope(this);
    }

    private void NotifyOrQueue(string collectionName, IRaisableEntry entry)
    {
        if (_suspendDepth > 0)
        {
            _suspendedCollections.Add(collectionName);
            return;
        }
        entry.RaiseValueChanged();
    }

    private sealed class SuspendScope : IDisposable
    {
        private readonly UnitSaveData _owner;
        private bool _disposed;

        public SuspendScope(UnitSaveData owner) => _owner = owner;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (--_owner._suspendDepth > 0) return;

            var pending = _owner._suspendedCollections.ToArray();
            _owner._suspendedCollections.Clear();
            foreach (var name in pending)
                _owner.OnPropertyChanged(name);
        }
    }

    // ===== INPC =====

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

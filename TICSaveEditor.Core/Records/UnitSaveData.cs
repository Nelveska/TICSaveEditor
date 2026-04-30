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
    public const int AbilityFlagJobCount = 22;

    /// <summary>
    /// Job byte → per-unit ability_flag slot index, or -1 when there is no mapping
    /// (monsters, Na-shi/Unknown placeholders, no-job sentinel, out-of-range bytes).
    /// </summary>
    /// <remarks>
    /// Slot is determined by the unit's class NAME, per <c>Docs/JobList.md</c>
    /// (user-supplied 2026-04-29) and confirmed by 3 independent community sources:
    /// <list type="bullet">
    ///   <item>Canonical generic class names (Squire..Mime) → slots 0..19 by name.</item>
    ///   <item>Dark Knight → slot 20. Onion Knight → slot 21. Both classes are
    ///         disabled in TIC but the slots are reserved (the engine inherits the
    ///         layout from a version that shipped them).</item>
    ///   <item>Story-unique class names (Holy Knight, Sword Saint, Machinist,
    ///         Templar, Princess, Dragonkin, etc.) ALL share slot 0 with Squire.
    ///         The game interprets the bits per the unit's CURRENT class, so
    ///         Agrias's Holy Sword learned-flags live in slot 0 and the game
    ///         renders them as Holy Sword while she is a Holy Knight. JP storage
    ///         works the same way.</item>
    ///   <item>Multiple Job IDs per unique class name (e.g. 3 Holy Knights) are
    ///         visibility/chapter variants — same storage location.</item>
    ///   <item>Monsters / Na-shi / Unknown / no-job sentinel → no slot.</item>
    /// </list>
    /// <para>
    /// Mod-edge caveat: because all story-unique classes share slot 0, a hypothetical
    /// mod that gave a single character two non-generic skillsets simultaneously
    /// would see learned-bit edits for skillset A also flip the matching bit of
    /// skillset B (same address). Vanilla-faithful; documented but not handled.
    /// See <c>decisions_jobbyte_vs_abilityflag_index.md</c>.
    /// </para>
    /// </remarks>
    private static readonly sbyte[] JobByteToAbilityFlagSlot = BuildJobSlotTable();

    private static sbyte[] BuildJobSlotTable()
    {
        var t = new sbyte[256];
        Array.Fill<sbyte>(t, -1);

        // Slot 0 = Squire / story Squire variants / ALL story-unique class names.
        // (See xmldoc above for the class-name rule and slot-0 fallback for
        //  story-unique classes.)
        ReadOnlySpan<byte> slot0 = stackalloc byte[]
        {
            // Story Squire variants (Ramza Ch.1, Ramza Ch.2 & 3, Delita Ch.1, Argath)
            0x01, 0x02, 0x04, 0x07,
            // Story-unique class names (Gallant Knight, Holy Knight, Ark Knight,
            // Rune Knight, Duke, Princess, Sword Saint, High Confessor, Dragonkin,
            // Celebrant, Fell Knight, Netherseer, Elder, Cleric, Astrologer,
            // Machinist, Cardinal, Skyseer, Commoner, Grand Duke, Holy Knight,
            // Templar, White Knight, Witch of the Coven, Machinist, Viscount,
            // Divine Knight, Nightblade, Sorcerer, White Knight, Skyseer,
            // Divine Knight, Machinist, Cleric, Assassin, Divine Knight,
            // Cleric, False Saint, Soldier, Ark Knight, Holy Knight)
            0x03, 0x05, 0x06, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E,
            0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0x21, 0x22,
            0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C,
            0x2D, 0x2E, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34,
            // Boss/special unique classes (Gigas, Death Seraph, Bringer of Order,
            // High Seraph, The Impure, The Wroth, Holy Dragon, Arch Seraph)
            0x3C, 0x3E, 0x40, 0x41, 0x43, 0x45, 0x48, 0x49,
            // Late-range unique (Byblos, Automaton, Reaver, Serpentarius,
            // Holy Dragon, Archaeodaemon, Ultima Demon)
            0x90, 0x91, 0x96, 0x97, 0x98, 0x99, 0x9A,
            // 0xA-range unique (Sky Pirate, Game Hunter, Deathknight,
            // Templar, Celebrant, Dark Dragon)
            0xA2, 0xA3, 0xA5, 0xA6, 0xA7, 0xA8,
            // Canonical Squire generic
            0x4A,
        };
        foreach (byte b in slot0) t[b] = 0;

        // Canonical generics by class name.
        ReadOnlySpan<byte> chemist     = stackalloc byte[] { 0x35, 0x4B };
        ReadOnlySpan<byte> knight      = stackalloc byte[] { 0x3D, 0x4C };
        ReadOnlySpan<byte> archer      = stackalloc byte[] { 0x3F, 0x4D };
        ReadOnlySpan<byte> whiteMage   = stackalloc byte[] { 0x36, 0x4F };
        ReadOnlySpan<byte> blackMage   = stackalloc byte[] { 0x37, 0x42, 0x50 };
        ReadOnlySpan<byte> timeMage    = stackalloc byte[] { 0x44, 0x51 };
        ReadOnlySpan<byte> summoner    = stackalloc byte[] { 0x47, 0x52 };
        ReadOnlySpan<byte> mystic      = stackalloc byte[] { 0x38, 0x46, 0x55 };
        foreach (byte b in chemist)   t[b] = 1;
        foreach (byte b in knight)    t[b] = 2;
        foreach (byte b in archer)    t[b] = 3;
        t[0x4E] = 4;                              // Monk
        foreach (byte b in whiteMage) t[b] = 5;
        foreach (byte b in blackMage) t[b] = 6;
        foreach (byte b in timeMage)  t[b] = 7;
        foreach (byte b in summoner)  t[b] = 8;
        t[0x53] = 9;                              // Thief
        t[0x54] = 10;                             // Orator
        foreach (byte b in mystic)    t[b] = 11;
        t[0x56] = 12;                             // Geomancer
        t[0x57] = 13;                             // Dragoon
        t[0x58] = 14;                             // Samurai
        t[0x59] = 15;                             // Ninja
        t[0x5A] = 16;                             // Arithmetician
        t[0x5B] = 17;                             // Bard
        t[0x5C] = 18;                             // Dancer
        t[0x5D] = 19;                             // Mime

        // Slots 20-21: Dark Knight + Onion Knight (disabled in TIC, slot-reserved).
        t[0xA0] = 20;                             // Dark Knight
        t[0xA1] = 21;                             // Onion Knight
        t[0xA4] = 21;                             // Onion Knight (variant)

        // -1 (unchanged) for: 0x00 (no-job sentinel), 0x39/0x3A/0x3B/0x9B..0x9F
        // (Unknown class), 0x5E..0x8D (monsters), 0x8E/0x8F/0x92..0x95 (Na-shi
        // placeholders), 0xA9..0xFF (out of JobList range).
        return t;
    }

    /// <summary>
    /// Resolve a save-format Job byte to the per-unit ability_flag slot index
    /// (0..21), or null if the job has no mapping (unique story-character class,
    /// monster, no-job sentinel, etc.). Pure save-format lookup; no GameDataContext.
    /// </summary>
    public static int? GetAbilityFlagSlotForJob(byte jobByte)
    {
        int slot = JobByteToAbilityFlagSlot[jobByte];
        return slot < 0 ? (int?)null : slot;
    }
    private const int AbilityFlagBytesPerJob = 3;
    private const int UnitNicknameLength = 16;
    private const int CustomJobNameLength = 16;
    private const int UnitNameTrailingLength = 32;
    private const int MaxStatBase = 0xFFFFFF;
    private const int CombatSetCount = 3;

    private UnitSaveDataLayout _layout;

    private readonly EquipItemEntry[] _equipItemEntries;
    private readonly JobLevelEntry[] _jobLevelEntries;
    private readonly JobPointEntry[] _jobPointEntries;
    private readonly TotalJobPointEntry[] _totalJobPointEntries;
    private readonly JobAbilityFlagsEntry[] _abilityFlagsEntries;
    private readonly CombatSet[] _combatSetEntries;

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

        _combatSetEntries = new CombatSet[CombatSetCount];
        for (int i = 0; i < CombatSetCount; i++) _combatSetEntries[i] = new CombatSet(this, i);
        CombatSets = new ReadOnlyObservableCollection<CombatSet>(
            new ObservableCollection<CombatSet>(_combatSetEntries));
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

    public byte UnitIndex
    {
        get => _layout.UnitIndex;
        set { if (_layout.UnitIndex == value) return; _layout.UnitIndex = value; OnPropertyChanged(); }
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

    public byte Birthday
    {
        get => _layout.Birthday;
        set { if (_layout.Birthday == value) return; _layout.Birthday = value; OnPropertyChanged(); }
    }

    public byte ZodiacSign
    {
        get => _layout.ZodiacSign;
        set { if (_layout.ZodiacSign == value) return; _layout.ZodiacSign = value; OnPropertyChanged(); }
    }

    public byte SecondaryAction
    {
        get => _layout.SecondaryAction;
        set { if (_layout.SecondaryAction == value) return; _layout.SecondaryAction = value; OnPropertyChanged(); }
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

    public ushort MovementAbility
    {
        get => _layout.MovementAbility;
        set { if (_layout.MovementAbility == value) return; _layout.MovementAbility = value; OnPropertyChanged(); }
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

    public int UnlockedJobs
    {
        get { unsafe { fixed (byte* p = _layout.UnlockedJobs) return Read24(p); } }
        set { ValidateStatBase(value); unsafe { fixed (byte* p = _layout.UnlockedJobs) Write24(p, value); } OnPropertyChanged(); }
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

    /// <summary>
    /// 16 bytes at offset 0xDC holding the player-set rename string (ASCII,
    /// null-terminated). The community TIC struct names this sub-field
    /// <c>UnitNickname</c>; see <c>decisions_chr_name_rename_storage.md</c>.
    /// </summary>
    public byte[] UnitNicknameRaw
    {
        get
        {
            var copy = new byte[UnitNicknameLength];
            unsafe
            {
                fixed (byte* p = _layout.UnitNickname)
                {
                    new ReadOnlySpan<byte>(p, UnitNicknameLength).CopyTo(copy);
                }
            }
            return copy;
        }
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (value.Length != UnitNicknameLength)
                throw new ArgumentException(
                    $"UnitNicknameRaw must be exactly {UnitNicknameLength} bytes (got {value.Length}).",
                    nameof(value));

            unsafe
            {
                fixed (byte* p = _layout.UnitNickname)
                {
                    value.AsSpan().CopyTo(new Span<byte>(p, UnitNicknameLength));
                }
            }
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 16 bytes at offset 0xEC. Empirically zero-filled across all observed
    /// real saves; community TIC struct names this <c>CustomJobName</c>.
    /// Exposed for byte-faithful round-trip; no v0.1 rendering uses these bytes.
    /// </summary>
    public byte[] CustomJobNameRaw
    {
        get
        {
            var copy = new byte[CustomJobNameLength];
            unsafe
            {
                fixed (byte* p = _layout.CustomJobName)
                {
                    new ReadOnlySpan<byte>(p, CustomJobNameLength).CopyTo(copy);
                }
            }
            return copy;
        }
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (value.Length != CustomJobNameLength)
                throw new ArgumentException(
                    $"CustomJobNameRaw must be exactly {CustomJobNameLength} bytes (got {value.Length}).",
                    nameof(value));

            unsafe
            {
                fixed (byte* p = _layout.CustomJobName)
                {
                    value.AsSpan().CopyTo(new Span<byte>(p, CustomJobNameLength));
                }
            }
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 32 bytes at offset 0xFC, anonymous in the community TIC struct
    /// (<c>field_FC</c>). Exposed for byte-faithful round-trip; no v0.1
    /// rendering uses these bytes.
    /// </summary>
    public byte[] UnitNameTrailingRaw
    {
        get
        {
            var copy = new byte[UnitNameTrailingLength];
            unsafe
            {
                fixed (byte* p = _layout.UnitNameTrailing)
                {
                    new ReadOnlySpan<byte>(p, UnitNameTrailingLength).CopyTo(copy);
                }
            }
            return copy;
        }
        set
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (value.Length != UnitNameTrailingLength)
                throw new ArgumentException(
                    $"UnitNameTrailingRaw must be exactly {UnitNameTrailingLength} bytes (got {value.Length}).",
                    nameof(value));

            unsafe
            {
                fixed (byte* p = _layout.UnitNameTrailing)
                {
                    value.AsSpan().CopyTo(new Span<byte>(p, UnitNameTrailingLength));
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

    public byte CurrentCombatSet
    {
        get => _layout.CurrentCombatSet;
        set { if (_layout.CurrentCombatSet == value) return; _layout.CurrentCombatSet = value; OnPropertyChanged(); }
    }

    public ushort CharaNameKey
    {
        get => _layout.CharaNameKey;
        set { if (_layout.CharaNameKey == value) return; _layout.CharaNameKey = value; OnPropertyChanged(); }
    }

    // ===== CombatSets (3 × 88 bytes at 0x126..0x22D) =====

    public ReadOnlyObservableCollection<CombatSet> CombatSets { get; }

    private ref CombatSetLayout GetCombatSetRef(int index)
    {
        switch (index)
        {
            case 0: return ref _layout.CombatSet0;
            case 1: return ref _layout.CombatSet1;
            case 2: return ref _layout.CombatSet2;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    internal string GetCombatSetName(int index)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetCombatSetRef(index);
        unsafe
        {
            fixed (byte* p = slot.Name)
            {
                var span = new ReadOnlySpan<byte>(p, CombatSet.NameByteLength);
                var nullIdx = span.IndexOf((byte)0);
                var len = nullIdx < 0 ? CombatSet.NameByteLength : nullIdx;
                return Encoding.ASCII.GetString(span.Slice(0, len));
            }
        }
    }

    internal void SetCombatSetName(int index, string value)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetCombatSetRef(index);
        var v = value ?? string.Empty;
        var clampedLen = Math.Min(v.Length, CombatSet.NameByteLength - 1);

        unsafe
        {
            fixed (byte* p = slot.Name)
            {
                var dest = new Span<byte>(p, CombatSet.NameByteLength);
                var bytesWritten = Encoding.ASCII.GetBytes(v.AsSpan(0, clampedLen), dest);
                if (bytesWritten < CombatSet.NameByteLength)
                {
                    dest[bytesWritten] = 0;
                }
            }
        }
        NotifyOrQueue(nameof(CombatSets), _combatSetEntries[index]);
    }

    internal byte GetCombatSetJob(int index)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetCombatSetRef(index).Job;
    }

    internal void SetCombatSetJob(int index, byte value)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetCombatSetRef(index);
        if (slot.Job == value) return;
        slot.Job = value;
        NotifyOrQueue(nameof(CombatSets), _combatSetEntries[index]);
    }

    internal bool GetCombatSetIsDoubleHand(int index)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return GetCombatSetRef(index).IsDoubleHand != 0;
    }

    internal void SetCombatSetIsDoubleHand(int index, bool value)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetCombatSetRef(index);
        byte newByte = value ? (byte)1 : (byte)0;
        if (slot.IsDoubleHand == newByte) return;
        slot.IsDoubleHand = newByte;
        NotifyOrQueue(nameof(CombatSets), _combatSetEntries[index]);
    }

    internal byte[] GetCombatSetItemBytes(int index)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetCombatSetRef(index);
        var copy = new byte[CombatSet.ItemBytesLength];
        unsafe
        {
            fixed (byte* p = slot.ItemBytes)
            {
                new ReadOnlySpan<byte>(p, CombatSet.ItemBytesLength).CopyTo(copy);
            }
        }
        return copy;
    }

    internal byte[] GetCombatSetAbilityBytes(int index)
    {
        if ((uint)index >= (uint)CombatSetCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        ref var slot = ref GetCombatSetRef(index);
        var copy = new byte[CombatSet.AbilityBytesLength];
        unsafe
        {
            fixed (byte* p = slot.AbilityBytes)
            {
                new ReadOnlySpan<byte>(p, CombatSet.AbilityBytesLength).CopyTo(copy);
            }
        }
        return copy;
    }

    // ===== Empty / active detection =====

    public bool IsEmpty => _layout.Character == 0;

    /// <summary>
    /// Returns true if this unit is currently active in the player's party at the
    /// given slot index. <see cref="UnitIndex"/> (offset 0x01) holds the unit's
    /// own slot index when active and 0xFF when the unit is inactive (departed
    /// guest, dismissed recruit, stowed) or the slot is empty. Verified
    /// 2026-04-30 against <c>SaveFiles/enhanced.png</c>; see
    /// <c>decisions_unit_index_active_flag.md</c>.
    /// </summary>
    public bool IsInActiveParty(int ownSlotIndex)
    {
        if (_layout.Character == 0) return false;
        return _layout.UnitIndex == (byte)ownSlotIndex;
    }

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
        var zodiacSign = (_layout.ZodiacSign & 0xF0) >> 4;
        if (zodiacSign > 11)
            issues.Add(new ValidationIssue(nameof(ZodiacSign),
                $"Zodiac sign must be in [0, 11] (got high-nibble {zodiacSign} from byte 0x{_layout.ZodiacSign:X2})."));

        if (_layout.CurrentCombatSet > 2)
            issues.Add(new ValidationIssue(
                nameof(CurrentCombatSet),
                $"CurrentCombatSet must be in [0, 2] (got {_layout.CurrentCombatSet})."));

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

    /// <summary>
    /// Learn every ability of the given ability-flag slot.
    /// <para>
    /// NOTE: <paramref name="abilityFlagIndex"/> is the 0-based slot in the
    /// 22-entry ability_flag array, NOT the unit's <see cref="Job"/> byte. The
    /// save-format Job byte is JobData-table-aligned (JobData ID 0 is a blank
    /// entry; generic jobs are IDs 1–22), but ability_flag storage is dense
    /// [0..21] starting at Squire. Use <see cref="TryLearnAllAbilitiesForCurrentJob"/>
    /// when you want "learn the abilities of this unit's current job" — it
    /// handles the off-by-one and the no-job / story-character cases.
    /// </para>
    /// </summary>
    public void LearnAllAbilitiesForJob(int abilityFlagIndex)
    {
        if ((uint)abilityFlagIndex >= (uint)AbilityFlagJobCount)
            throw new ArgumentOutOfRangeException(nameof(abilityFlagIndex));

        using var _ = SuspendNotifications();
        for (int b = 0; b < AbilityFlagBytesPerJob; b++)
            SetAbilityFlagByte(abilityFlagIndex, b, 0xFF);
    }

    /// <summary>
    /// Forget every ability of the given ability-flag slot.
    /// See <see cref="LearnAllAbilitiesForJob"/> for the index/Job-byte distinction.
    /// </summary>
    public void ForgetAllAbilitiesForJob(int abilityFlagIndex)
    {
        if ((uint)abilityFlagIndex >= (uint)AbilityFlagJobCount)
            throw new ArgumentOutOfRangeException(nameof(abilityFlagIndex));

        using var _ = SuspendNotifications();
        for (int b = 0; b < AbilityFlagBytesPerJob; b++)
            SetAbilityFlagByte(abilityFlagIndex, b, 0x00);
    }

    /// <summary>
    /// Learn every ability of the unit's current job. Resolves the save-format
    /// <see cref="Job"/> byte to its per-unit ability_flag slot via
    /// <see cref="GetAbilityFlagSlotForJob"/>. Returns false (no mutation) when
    /// the job has no mapping — story-character unique classes, monsters, no-job
    /// sentinel, etc.
    /// </summary>
    public bool TryLearnAllAbilitiesForCurrentJob()
    {
        if (GetAbilityFlagSlotForJob(Job) is not int slot) return false;
        LearnAllAbilitiesForJob(slot);
        return true;
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

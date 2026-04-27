using System.Collections.ObjectModel;
using System.ComponentModel;
using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class BattleSection : SaveWorkSection
{
    public const int UnitCount = 54;

    public const int PartyItemLength = 0x105;
    public const int ShopItemLength = 0x105;
    public const int FindItemLength = 0x80;
    public const int EventWorkByteLength = 0x400;

    private const int UnitsBytes = UnitCount * UnitSaveData.Size;

    // All offsets are relative to the start of BattleSection.Bytes.
    private const int TrailingOffset = UnitsBytes;
    private const int TrailingLength = SaveWorkLayout.BattleSize - UnitsBytes;

    private const int PartyItemOffset = TrailingOffset;
    private const int ShopItemOffset = PartyItemOffset + PartyItemLength;
    private const int FindItemOffset = ShopItemOffset + ShopItemLength;
    private const int EventWorkOffset = FindItemOffset + FindItemLength;
    private const int BattleSortOffset = EventWorkOffset + EventWorkByteLength;
    private const int BattleSortLength =
        TrailingLength - PartyItemLength - ShopItemLength - FindItemLength - EventWorkByteLength;

    private readonly UnitSaveData[] _units;
    private readonly byte[] _partyItemRaw;
    private readonly byte[] _shopItemRaw;
    private readonly byte[] _findItemRaw;
    private readonly EventWork _eventWork;
    private readonly byte[] _battleSortRaw;

    internal BattleSection(ReadOnlySpan<byte> bytes) : base(bytes)
    {
        _units = new UnitSaveData[UnitCount];
        for (int i = 0; i < UnitCount; i++)
        {
            _units[i] = new UnitSaveData(bytes.Slice(i * UnitSaveData.Size, UnitSaveData.Size));
            _units[i].PropertyChanged += OnUnitPropertyChanged;
        }
        Units = new ReadOnlyCollection<UnitSaveData>(_units);

        _partyItemRaw = bytes.Slice(PartyItemOffset, PartyItemLength).ToArray();
        _shopItemRaw  = bytes.Slice(ShopItemOffset,  ShopItemLength).ToArray();
        _findItemRaw  = bytes.Slice(FindItemOffset,  FindItemLength).ToArray();
        _eventWork    = new EventWork(bytes.Slice(EventWorkOffset, EventWorkByteLength));
        _battleSortRaw = bytes.Slice(BattleSortOffset, BattleSortLength).ToArray();

        PartyInventory = new PartyInventory(_partyItemRaw);
        ShopInventory = new ShopInventory(_shopItemRaw);
        FoundItems = new FoundItemCollection(_findItemRaw);
    }

    /// <summary>
    /// Inventory wrapper over <see cref="PartyItemRaw"/>. Operates on the same byte array
    /// (single source of truth); mutations through this wrapper are visible via PartyItemRaw
    /// and serialize correctly through <see cref="WriteTo"/>. Storage indices are byte
    /// positions [0, 0x105) — see decisions_m9_wrapper_api_storage_index.md.
    /// </summary>
    public PartyInventory PartyInventory { get; }

    /// <summary>
    /// Inventory wrapper over <see cref="ShopItemRaw"/>. Same shape as
    /// <see cref="PartyInventory"/>.
    /// </summary>
    public ShopInventory ShopInventory { get; }

    /// <summary>
    /// Inventory wrapper over <see cref="FindItemRaw"/>. Smaller capacity (0x80).
    /// </summary>
    public FoundItemCollection FoundItems { get; }

    internal override int Size => SaveWorkLayout.BattleSize;

    public IReadOnlyList<UnitSaveData> Units { get; }

    /// <summary>
    /// 261-byte party-inventory region (template <c>battle.PartyItem[0x105]</c>). One byte per
    /// item-storage slot (count). The byte index is category-grouped storage, NOT a direct
    /// item ID — see decisions_battlesection_inventory_raw_passthrough.md. Wrapper class with
    /// itemId↔storageIndex mapping arrives in M7 with GameDataContext.
    /// </summary>
    public byte[] PartyItemRaw
    {
        get
        {
            var copy = new byte[PartyItemLength];
            _partyItemRaw.CopyTo(copy.AsSpan());
            return copy;
        }
    }

    /// <summary>
    /// 261-byte shop-inventory region (template <c>battle.ShopItem[0x105]</c>). Same indexing
    /// caveats as <see cref="PartyItemRaw"/>.
    /// </summary>
    public byte[] ShopItemRaw
    {
        get
        {
            var copy = new byte[ShopItemLength];
            _shopItemRaw.CopyTo(copy.AsSpan());
            return copy;
        }
    }

    /// <summary>
    /// 128-byte found-items region (template <c>battle.FindItem[0x80]</c>). Same indexing
    /// caveats as <see cref="PartyItemRaw"/>.
    /// </summary>
    public byte[] FindItemRaw
    {
        get
        {
            var copy = new byte[FindItemLength];
            _findItemRaw.CopyTo(copy.AsSpan());
            return copy;
        }
    }

    /// <summary>
    /// 256 × int32 LE event-work variables (template <c>battle.eventwork[0x100]</c>),
    /// read-only opaque per spec §6.7. Mutation surface deferred until indices have
    /// documented meaning.
    /// </summary>
    public EventWork EventWork => _eventWork;

    /// <summary>
    /// 2,606-byte trailing region after eventwork. Holds <c>battle.*_sort</c> +
    /// <c>battle.ffto_item_sort</c> arrays — internal field offsets not pinned by
    /// format-notes; preserved verbatim for byte-faithful round-trip.
    /// </summary>
    public byte[] BattleSortRaw
    {
        get
        {
            var copy = new byte[BattleSortLength];
            _battleSortRaw.CopyTo(copy.AsSpan());
            return copy;
        }
    }

    internal override void WriteTo(Span<byte> destination)
    {
        if (destination.Length != Size)
        {
            throw new ArgumentException(
                $"BattleSection.WriteTo requires exactly {Size} bytes (got {destination.Length}).",
                nameof(destination));
        }

        for (int i = 0; i < UnitCount; i++)
        {
            _units[i].WriteTo(destination.Slice(i * UnitSaveData.Size, UnitSaveData.Size));
        }

        _partyItemRaw.CopyTo(destination.Slice(PartyItemOffset, PartyItemLength));
        _shopItemRaw .CopyTo(destination.Slice(ShopItemOffset,  ShopItemLength));
        _findItemRaw .CopyTo(destination.Slice(FindItemOffset,  FindItemLength));
        _eventWork.WriteTo(destination.Slice(EventWorkOffset, EventWorkByteLength));
        _battleSortRaw.CopyTo(destination.Slice(BattleSortOffset, BattleSortLength));
    }

    internal override void RehydrateFrom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Size)
        {
            throw new ArgumentException(
                $"BattleSection.RehydrateFrom requires exactly {Size} bytes (got {bytes.Length}).",
                nameof(bytes));
        }

        for (int i = 0; i < UnitCount; i++)
        {
            _units[i].RehydrateFrom(bytes.Slice(i * UnitSaveData.Size, UnitSaveData.Size));
        }

        bytes.Slice(PartyItemOffset, PartyItemLength).CopyTo(_partyItemRaw);
        bytes.Slice(ShopItemOffset,  ShopItemLength) .CopyTo(_shopItemRaw);
        bytes.Slice(FindItemOffset,  FindItemLength) .CopyTo(_findItemRaw);
        _eventWork.RehydrateFrom(bytes.Slice(EventWorkOffset, EventWorkByteLength));
        bytes.Slice(BattleSortOffset, BattleSortLength).CopyTo(_battleSortRaw);

        OnPropertyChanged(null);
    }

    private void OnUnitPropertyChanged(object? sender, PropertyChangedEventArgs e)
        => OnPropertyChanged(nameof(Units));
}

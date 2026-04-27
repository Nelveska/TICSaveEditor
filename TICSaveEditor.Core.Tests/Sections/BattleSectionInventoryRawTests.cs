using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class BattleSectionInventoryRawTests
{
    private const int UnitsBytes = 54 * 600;
    private const int PartyItemOffset = UnitsBytes;
    private const int ShopItemOffset  = PartyItemOffset + 0x105;
    private const int FindItemOffset  = ShopItemOffset  + 0x105;
    private const int EventWorkOffset = FindItemOffset  + 0x80;
    private const int BattleSortOffset = EventWorkOffset + 0x400;

    private static byte[] BlankBattleBytes() => new byte[SaveWorkLayout.BattleSize];

    [Fact]
    public void PartyItemRaw_length_is_0x105()
    {
        var battle = new BattleSection(BlankBattleBytes());
        Assert.Equal(0x105, battle.PartyItemRaw.Length);
    }

    [Fact]
    public void ShopItemRaw_length_is_0x105()
    {
        var battle = new BattleSection(BlankBattleBytes());
        Assert.Equal(0x105, battle.ShopItemRaw.Length);
    }

    [Fact]
    public void FindItemRaw_length_is_0x80()
    {
        var battle = new BattleSection(BlankBattleBytes());
        Assert.Equal(0x80, battle.FindItemRaw.Length);
    }

    [Fact]
    public void BattleSortRaw_length_is_2606_bytes()
    {
        var battle = new BattleSection(BlankBattleBytes());
        Assert.Equal(2606, battle.BattleSortRaw.Length);
    }

    [Fact]
    public void PartyItemRaw_returns_defensive_copy()
    {
        var bytes = BlankBattleBytes();
        bytes[PartyItemOffset] = 0xAA;
        bytes[PartyItemOffset + 0x104] = 0xBB;
        var battle = new BattleSection(bytes);

        var first = battle.PartyItemRaw;
        first[0] = 0xFF;

        var second = battle.PartyItemRaw;
        Assert.Equal(0xAA, second[0]);
        Assert.Equal(0xBB, second[0x104]);
    }

    [Fact]
    public void Each_named_region_reads_at_its_documented_offset()
    {
        var bytes = BlankBattleBytes();
        bytes[PartyItemOffset]    = 0x11;
        bytes[ShopItemOffset]     = 0x22;
        bytes[FindItemOffset]     = 0x33;
        bytes[EventWorkOffset]    = 0x44;  // EventWork int32 LE byte 0
        bytes[BattleSortOffset]   = 0x55;
        var battle = new BattleSection(bytes);

        Assert.Equal(0x11, battle.PartyItemRaw[0]);
        Assert.Equal(0x22, battle.ShopItemRaw[0]);
        Assert.Equal(0x33, battle.FindItemRaw[0]);
        Assert.Equal(0x44, battle.EventWork.Get(0));
        Assert.Equal(0x55, battle.BattleSortRaw[0]);
    }

    [Fact]
    public void EventWork_property_reflects_constructed_bytes()
    {
        var bytes = BlankBattleBytes();
        // Write int32 LE 0x12345678 at the EventWork region's first slot.
        bytes[EventWorkOffset + 0] = 0x78;
        bytes[EventWorkOffset + 1] = 0x56;
        bytes[EventWorkOffset + 2] = 0x34;
        bytes[EventWorkOffset + 3] = 0x12;
        var battle = new BattleSection(bytes);

        Assert.Equal(0x12345678, battle.EventWork.Get(0));
    }

    [Fact]
    public void Random_bytes_round_trip_byte_identical_through_decomposed_trailing()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.BattleSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var battle = new BattleSection(bytes);

        var output = new byte[SaveWorkLayout.BattleSize];
        battle.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Last_byte_of_BattleSortRaw_is_at_battle_offset_36679()
    {
        var bytes = BlankBattleBytes();
        bytes[36679] = 0xEE;  // last byte of the section
        var battle = new BattleSection(bytes);

        Assert.Equal(0xEE, battle.BattleSortRaw[battle.BattleSortRaw.Length - 1]);
    }
}

using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class BattleSectionInventoryWrappersTests
{
    private static byte[] BlankBattleBytes() => new byte[SaveWorkLayout.BattleSize];

    [Fact]
    public void PartyInventory_GetCount_matches_PartyItemRaw_at_same_index()
    {
        var bytes = BlankBattleBytes();
        // Set a sentinel byte in PartyItem region (54 × 600 = 32400 = 0x7E90 trailing offset).
        bytes[0x7E90 + 0xF0] = 7;
        var battle = new BattleSection(bytes);

        Assert.Equal(7, battle.PartyInventory.GetCount(0xF0));
        Assert.Equal(7, battle.PartyItemRaw[0xF0]);
    }

    [Fact]
    public void Mutating_PartyInventory_propagates_to_WriteTo_output()
    {
        var battle = new BattleSection(BlankBattleBytes());
        battle.PartyInventory.SetCount(0x05, 9);

        var output = new byte[SaveWorkLayout.BattleSize];
        battle.WriteTo(output);

        Assert.Equal(9, output[0x7E90 + 0x05]);
    }

    [Fact]
    public void All_three_wrappers_share_byte_arrays_with_their_Raw_passthroughs()
    {
        var bytes = BlankBattleBytes();
        // Write sentinel bytes in each region.
        bytes[0x7E90 + 0x00] = 0xAA;             // PartyItem[0]
        bytes[0x7E90 + 0x105 + 0x00] = 0xBB;     // ShopItem[0]
        bytes[0x7E90 + 0x105 + 0x105 + 0x00] = 0xCC;  // FindItem[0]
        var battle = new BattleSection(bytes);

        Assert.Equal(0xAA, battle.PartyInventory.GetCount(0));
        Assert.Equal(0xBB, battle.ShopInventory.GetCount(0));
        Assert.Equal(0xCC, battle.FoundItems.GetCount(0));
    }

    [Fact]
    public void PartyInventory_uses_same_byte_array_as_PartyItemRaw_passthrough()
    {
        // Mutating via the wrapper updates what PartyItemRaw exposes (defensive copy reflects current state).
        var battle = new BattleSection(BlankBattleBytes());
        battle.PartyInventory.SetCount(0x42, 3);

        Assert.Equal(3, battle.PartyItemRaw[0x42]);
    }
}

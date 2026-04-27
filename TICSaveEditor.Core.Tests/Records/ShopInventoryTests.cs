using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class ShopInventoryTests
{
    [Fact]
    public void Capacity_is_0x105()
    {
        Assert.Equal(0x105, ShopInventory.Capacity);
    }

    [Fact]
    public void AllSlots_count_equals_Capacity()
    {
        var shop = new ShopInventory(new byte[ShopInventory.Capacity]);
        Assert.Equal(ShopInventory.Capacity, shop.AllSlots.Count);
    }

    [Fact]
    public void Constructor_throws_on_wrong_length()
    {
        Assert.Throws<ArgumentException>(() => new ShopInventory(new byte[100]));
    }

    [Fact]
    public void SetCount_writes_to_underlying_bytes()
    {
        var bytes = new byte[ShopInventory.Capacity];
        var shop = new ShopInventory(bytes);
        shop.SetCount(50, 7);
        Assert.Equal(7, bytes[50]);
    }

    [Fact]
    public void NonEmpty_filters_correctly()
    {
        var bytes = new byte[ShopInventory.Capacity];
        bytes[3] = 2;
        bytes[200] = 1;
        var shop = new ShopInventory(bytes);
        Assert.Equal(2, shop.NonEmpty.Count);
    }
}

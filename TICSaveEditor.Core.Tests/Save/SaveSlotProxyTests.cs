using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveSlotProxyTests
{
    private static SaveSlot MakeSlot()
    {
        var bytes = new byte[SaveWork.Size];
        var saveWork = new SaveWork(bytes);
        return new SaveSlot(0, saveWork);
    }

    [Fact]
    public void IsEmpty_true_when_card_magic_zero()
    {
        var slot = MakeSlot();
        Assert.True(slot.IsEmpty);
    }

    [Fact]
    public void IsEmpty_false_when_card_magic_nonzero()
    {
        var bytes = new byte[SaveWork.Size];
        bytes[0x00] = 0xEF;
        bytes[0x01] = 0xBE;
        var slot = new SaveSlot(0, new SaveWork(bytes));
        Assert.False(slot.IsEmpty);
    }

    [Fact]
    public void SlotTitle_proxies_card_title_get()
    {
        var bytes = new byte[SaveWork.Size];
        bytes[0x04] = (byte)'M';
        bytes[0x05] = (byte)'y';
        var slot = new SaveSlot(0, new SaveWork(bytes));
        Assert.Equal("My", slot.SlotTitle);
    }

    [Fact]
    public void SlotTitle_proxies_card_title_set()
    {
        var slot = MakeSlot();
        slot.SlotTitle = "ProxyTest";
        Assert.Equal("ProxyTest", slot.SaveWork.Card.Title);
    }

    [Fact]
    public void SaveTimestamp_proxies_card_save_timestamp()
    {
        var slot = MakeSlot();
        Assert.Equal(slot.SaveWork.Card.SaveTimestamp, slot.SaveTimestamp);
    }

    [Fact]
    public void Card_title_change_raises_slot_title_property_changed()
    {
        var slot = MakeSlot();
        var raised = new List<string?>();
        slot.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

        slot.SaveWork.Card.Title = "Notify";

        Assert.Contains(nameof(SaveSlot.SlotTitle), raised);
    }
}

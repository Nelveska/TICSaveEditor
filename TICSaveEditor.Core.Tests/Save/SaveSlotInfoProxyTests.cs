using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveSlotInfoProxyTests
{
    private static SaveSlot NewSlot()
    {
        var saveWork = new SaveWork(new byte[SaveWork.Size]);
        return new SaveSlot(0, saveWork);
    }

    [Fact]
    public void HeroNameRaw_proxies_to_Info()
    {
        var slot = NewSlot();
        var name = new byte[17];
        for (int i = 0; i < 17; i++) name[i] = (byte)(i + 0x40);

        slot.HeroNameRaw = name;

        Assert.Equal(name, slot.SaveWork.Info.HeroNameRaw);
        Assert.Equal(name, slot.HeroNameRaw);
    }

    [Fact]
    public void Playtime_proxies_to_Info()
    {
        var slot = NewSlot();
        slot.Playtime = TimeSpan.FromHours(3);

        Assert.Equal(TimeSpan.FromHours(3), slot.SaveWork.Info.Playtime);
        Assert.Equal(TimeSpan.FromHours(3), slot.Playtime);
    }

    [Fact]
    public void HeroNameRaw_change_via_Info_re_raises_on_slot()
    {
        var slot = NewSlot();
        var hits = 0;
        slot.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SaveSlot.HeroNameRaw)) hits++;
        };

        var name = new byte[17];
        name[3] = 0x55;
        slot.SaveWork.Info.HeroNameRaw = name;

        Assert.Equal(1, hits);
    }

    [Fact]
    public void Playtime_change_via_Info_re_raises_on_slot()
    {
        var slot = NewSlot();
        var hits = 0;
        slot.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SaveSlot.Playtime)) hits++;
        };

        slot.SaveWork.Info.Playtime = TimeSpan.FromMinutes(15);

        Assert.Equal(1, hits);
    }
}

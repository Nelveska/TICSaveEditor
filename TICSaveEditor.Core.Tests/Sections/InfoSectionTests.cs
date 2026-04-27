using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class InfoSectionTests
{
    private static byte[] BlankBytes() => new byte[SaveWorkLayout.InfoSize];

    [Fact]
    public void HeroNameRaw_round_trips_byte_array()
    {
        var info = new InfoSection(BlankBytes());
        var name = new byte[17];
        for (int i = 0; i < 17; i++) name[i] = (byte)(i + 1);

        info.HeroNameRaw = name;

        Assert.Equal(name, info.HeroNameRaw);
    }

    [Fact]
    public void HeroNameRaw_setter_throws_on_wrong_length()
    {
        var info = new InfoSection(BlankBytes());
        Assert.Throws<ArgumentException>(() => info.HeroNameRaw = new byte[16]);
        Assert.Throws<ArgumentException>(() => info.HeroNameRaw = new byte[18]);
    }

    [Fact]
    public void HeroNameRaw_setter_throws_on_null()
    {
        var info = new InfoSection(BlankBytes());
        Assert.Throws<ArgumentNullException>(() => info.HeroNameRaw = null!);
    }

    [Fact]
    public void HeroNameRaw_returns_defensive_copy()
    {
        var info = new InfoSection(BlankBytes());
        var first = info.HeroNameRaw;
        first[0] = 0xFF;
        var second = info.HeroNameRaw;
        Assert.Equal(0, second[0]);
    }

    [Fact]
    public void NextEventId_reads_int32_LE_at_0x1C()
    {
        var bytes = BlankBytes();
        bytes[0x1C] = 0x78;
        bytes[0x1D] = 0x56;
        bytes[0x1E] = 0x34;
        bytes[0x1F] = 0x12;
        var info = new InfoSection(bytes);
        Assert.Equal(0x12345678, info.NextEventId);
    }

    [Fact]
    public void MainProgress_reads_int32_LE_at_0x20()
    {
        var bytes = BlankBytes();
        bytes[0x20] = 0x44;
        bytes[0x21] = 0x33;
        bytes[0x22] = 0x22;
        bytes[0x23] = 0x11;
        var info = new InfoSection(bytes);
        Assert.Equal(0x11223344, info.MainProgress);
    }

    [Fact]
    public void InternalChecksumRaw_returns_16_bytes_at_0x64()
    {
        var bytes = BlankBytes();
        for (int i = 0; i < 16; i++) bytes[0x64 + i] = (byte)(0xA0 | i);
        var info = new InfoSection(bytes);

        var sum = info.InternalChecksumRaw;
        Assert.Equal(16, sum.Length);
        Assert.Equal(0xA0, sum[0]);
        Assert.Equal(0xAF, sum[15]);
    }

    [Fact]
    public void Playtime_round_trips_as_TimeSpan_seconds()
    {
        var info = new InfoSection(BlankBytes());
        info.Playtime = TimeSpan.FromHours(12);

        Assert.Equal(TimeSpan.FromHours(12), info.Playtime);
    }

    [Fact]
    public void Playtime_setter_throws_on_negative()
    {
        var info = new InfoSection(BlankBytes());
        Assert.Throws<ArgumentOutOfRangeException>(() => info.Playtime = TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void Playtime_writes_int32_LE_at_0x74()
    {
        var info = new InfoSection(BlankBytes());
        info.Playtime = TimeSpan.FromSeconds(0x12345678);

        var output = new byte[SaveWorkLayout.InfoSize];
        info.WriteTo(output);

        Assert.Equal(0x78, output[0x74]);
        Assert.Equal(0x56, output[0x75]);
        Assert.Equal(0x34, output[0x76]);
        Assert.Equal(0x12, output[0x77]);
    }

    [Fact]
    public void InfoTrailingRaw_returns_64_bytes_at_0x78()
    {
        var bytes = BlankBytes();
        bytes[0x78] = 0xDE;
        bytes[0xB7] = 0xAD;
        var info = new InfoSection(bytes);

        var trailing = info.InfoTrailingRaw;
        Assert.Equal(64, trailing.Length);
        Assert.Equal(0xDE, trailing[0]);
        Assert.Equal(0xAD, trailing[63]);
    }

    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.InfoSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var info = new InfoSection(bytes);

        var output = new byte[SaveWorkLayout.InfoSize];
        info.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void HeroNameRaw_setter_fires_PropertyChanged()
    {
        var info = new InfoSection(BlankBytes());
        var hits = 0;
        info.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InfoSection.HeroNameRaw)) hits++;
        };

        var name = new byte[17];
        name[0] = 0x01;
        info.HeroNameRaw = name;

        Assert.Equal(1, hits);
    }

    [Fact]
    public void Playtime_setter_fires_PropertyChanged()
    {
        var info = new InfoSection(BlankBytes());
        var hits = 0;
        info.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(InfoSection.Playtime)) hits++;
        };

        info.Playtime = TimeSpan.FromMinutes(5);

        Assert.Equal(1, hits);
    }
}

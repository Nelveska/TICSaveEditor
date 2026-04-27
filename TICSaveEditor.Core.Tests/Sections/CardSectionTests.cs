using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class CardSectionTests
{
    private static byte[] BlankCardBytes() => new byte[SaveWorkLayout.CardSize];

    [Fact]
    public void Magic_zero_when_bytes_zero()
    {
        var card = new CardSection(BlankCardBytes());
        Assert.Equal((ushort)0, card.Magic);
    }

    [Fact]
    public void Magic_reads_low_two_bytes_little_endian()
    {
        var bytes = BlankCardBytes();
        bytes[0x00] = 0xEF;
        bytes[0x01] = 0xBE;
        var card = new CardSection(bytes);
        Assert.Equal((ushort)0xBEEF, card.Magic);
    }

    [Fact]
    public void Title_round_trips_ascii()
    {
        var card = new CardSection(BlankCardBytes());
        card.Title = "Hello";
        Assert.Equal("Hello", card.Title);
    }

    [Fact]
    public void Title_reads_until_first_null()
    {
        var bytes = BlankCardBytes();
        bytes[0x04] = (byte)'A';
        bytes[0x05] = (byte)'B';
        bytes[0x06] = (byte)'C';
        bytes[0x08] = (byte)'D';
        bytes[0x09] = (byte)'E';
        var card = new CardSection(bytes);
        Assert.Equal("ABC", card.Title);
    }

    [Fact]
    public void Title_clamps_at_max_length()
    {
        var card = new CardSection(BlankCardBytes());
        var longInput = new string('X', 100);
        card.Title = longInput;
        var read = card.Title;
        Assert.True(read.Length <= 0x40 - 1);
        Assert.All(read, c => Assert.Equal('X', c));
    }

    [Fact]
    public void SaveTimestamp_zero_bytes_returns_unix_epoch()
    {
        var card = new CardSection(BlankCardBytes());
        Assert.Equal(DateTime.UnixEpoch, card.SaveTimestamp);
    }

    [Fact]
    public void SaveTimestamp_reads_int32_seconds_from_offset_0x44()
    {
        var bytes = BlankCardBytes();
        // 1745539200 = 2025-04-25 00:00:00 UTC, seconds since epoch
        bytes[0x44] = 0x00;
        bytes[0x45] = 0x82;
        bytes[0x46] = 0x0B;
        bytes[0x47] = 0x68;
        var card = new CardSection(bytes);
        Assert.Equal(
            DateTimeOffset.FromUnixTimeSeconds(0x680B8200).UtcDateTime,
            card.SaveTimestamp);
    }

    [Fact]
    public void IconRaw_returns_128_bytes_from_offset_0x80()
    {
        var bytes = BlankCardBytes();
        bytes[0x80] = 0xAA;
        bytes[0xFF] = 0xBB;
        var card = new CardSection(bytes);
        var icon = card.IconRaw;
        Assert.Equal(0x80, icon.Length);
        Assert.Equal(0xAA, icon[0]);
        Assert.Equal(0xBB, icon[0x7F]);
    }

    [Fact]
    public void IconRaw_returns_independent_copy()
    {
        var card = new CardSection(BlankCardBytes());
        var icon1 = card.IconRaw;
        icon1[0] = 0x42;
        var icon2 = card.IconRaw;
        Assert.Equal(0x00, icon2[0]);
    }

    [Fact]
    public void Setting_title_preserves_magic_timestamp_and_icon_bytes()
    {
        var bytes = BlankCardBytes();
        bytes[0x00] = 0xEF;
        bytes[0x01] = 0xBE;
        bytes[0x44] = 0x11;
        bytes[0x80] = 0x55;
        var card = new CardSection(bytes);

        card.Title = "Different";

        var dest = new byte[SaveWorkLayout.CardSize];
        card.WriteTo(dest);

        Assert.Equal(0xEF, dest[0x00]);
        Assert.Equal(0xBE, dest[0x01]);
        Assert.Equal(0x11, dest[0x44]);
        Assert.Equal(0x55, dest[0x80]);
    }

    [Fact]
    public void Setting_title_raises_property_changed()
    {
        var card = new CardSection(BlankCardBytes());
        var raised = new List<string?>();
        card.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

        card.Title = "Triggered";

        Assert.Contains(nameof(CardSection.Title), raised);
    }
}

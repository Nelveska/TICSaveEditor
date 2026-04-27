using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveWorkTests
{
    [Fact]
    public void RoundTrip_with_no_edits_is_byte_identical()
    {
        var rng = new Random(42);
        var bytes = new byte[SaveWork.Size];
        rng.NextBytes(bytes);

        var saveWork = new SaveWork(bytes);
        var output = saveWork.RawBytes;

        Assert.Equal(bytes, output);
    }

    [Fact]
    public void Editing_card_title_writes_through_to_RawBytes_at_card_offset()
    {
        var saveWork = new SaveWork(new byte[SaveWork.Size]);
        saveWork.Card.Title = "Edited";

        var output = saveWork.RawBytes;
        var cardSlice = output.AsSpan(SaveWorkLayout.CardOffset, SaveWorkLayout.CardSize);

        Assert.Equal((byte)'E', cardSlice[0x04]);
        Assert.Equal((byte)'d', cardSlice[0x05]);
        Assert.Equal((byte)'i', cardSlice[0x06]);
        Assert.Equal((byte)'t', cardSlice[0x07]);
        Assert.Equal((byte)'e', cardSlice[0x08]);
        Assert.Equal((byte)'d', cardSlice[0x09]);
        Assert.Equal((byte)0, cardSlice[0x0A]);
    }

    [Fact]
    public void Editing_card_title_does_not_disturb_other_sections()
    {
        var rng = new Random(123);
        var bytes = new byte[SaveWork.Size];
        rng.NextBytes(bytes);
        var originalCopy = (byte[])bytes.Clone();

        var saveWork = new SaveWork(bytes);
        saveWork.Card.Title = "X";

        var output = saveWork.RawBytes;
        // Bytes outside CardSection should match original.
        Assert.Equal(
            originalCopy.AsSpan(SaveWorkLayout.InfoOffset).ToArray(),
            output.AsSpan(SaveWorkLayout.InfoOffset).ToArray());
    }

    [Fact]
    public void Editing_difficulty_writes_through_to_RawBytes_at_fftoconfig_offset()
    {
        var saveWork = new SaveWork(new byte[SaveWork.Size]);
        saveWork.FftoConfig.DifficultyLevel = 3;

        var output = saveWork.RawBytes;
        Assert.Equal((byte)3, output[SaveWorkLayout.FftoConfigOffset]);
    }

    [Fact]
    public void Editing_difficulty_preserves_neighbor_sections()
    {
        var bytes = new byte[SaveWork.Size];
        // Mark FftoAchievement (which precedes FftoConfig) and FftoBraveStory (follows)
        bytes[SaveWorkLayout.FftoAchievementOffset] = 0xAA;
        bytes[SaveWorkLayout.FftoAchievementOffset + SaveWorkLayout.FftoAchievementSize - 1] = 0xBB;
        bytes[SaveWorkLayout.FftoBraveStoryOffset] = 0xCC;

        var saveWork = new SaveWork(bytes);
        saveWork.FftoConfig.DifficultyLevel = 9;

        var output = saveWork.RawBytes;
        Assert.Equal(0xAA, output[SaveWorkLayout.FftoAchievementOffset]);
        Assert.Equal(0xBB, output[SaveWorkLayout.FftoAchievementOffset + SaveWorkLayout.FftoAchievementSize - 1]);
        Assert.Equal(0xCC, output[SaveWorkLayout.FftoBraveStoryOffset]);
        Assert.Equal((byte)9, output[SaveWorkLayout.FftoConfigOffset]);
    }

    [Fact]
    public void Trailing_unk_bytes_are_preserved_through_round_trip()
    {
        var bytes = new byte[SaveWork.Size];
        for (var i = 0; i < SaveWorkLayout.TrailingUnkSize; i++)
        {
            bytes[SaveWorkLayout.TrailingUnkOffset + i] = (byte)(0x40 + i);
        }

        var saveWork = new SaveWork(bytes);
        var output = saveWork.RawBytes;

        for (var i = 0; i < SaveWorkLayout.TrailingUnkSize; i++)
        {
            Assert.Equal(
                (byte)(0x40 + i),
                output[SaveWorkLayout.TrailingUnkOffset + i]);
        }
    }

    [Fact]
    public void Constructor_rejects_wrong_size()
    {
        Assert.Throws<ArgumentException>(() => new SaveWork(new byte[100]));
        Assert.Throws<ArgumentException>(() => new SaveWork(new byte[SaveWork.Size + 1]));
    }
}

using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class FftoConfigSectionTests
{
    [Fact]
    public void DifficultyLevel_zero_when_bytes_zero()
    {
        var section = new FftoConfigSection(new byte[SaveWorkLayout.FftoConfigSize]);
        Assert.Equal((byte)0, section.DifficultyLevel);
    }

    [Fact]
    public void DifficultyLevel_reads_from_offset_zero()
    {
        var section = new FftoConfigSection(new byte[] { 0x05 });
        Assert.Equal((byte)5, section.DifficultyLevel);
    }

    [Fact]
    public void DifficultyLevel_round_trips()
    {
        var section = new FftoConfigSection(new byte[SaveWorkLayout.FftoConfigSize]);
        section.DifficultyLevel = 3;
        Assert.Equal((byte)3, section.DifficultyLevel);
    }

    [Fact]
    public void Setting_difficulty_raises_property_changed()
    {
        var section = new FftoConfigSection(new byte[SaveWorkLayout.FftoConfigSize]);
        var raised = new List<string?>();
        section.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

        section.DifficultyLevel = 7;

        Assert.Contains(nameof(FftoConfigSection.DifficultyLevel), raised);
    }

    [Fact]
    public void Setting_same_value_does_not_raise_property_changed()
    {
        var bytes = new byte[] { 4 };
        var section = new FftoConfigSection(bytes);
        var raised = false;
        section.PropertyChanged += (s, e) => raised = true;

        section.DifficultyLevel = 4;

        Assert.False(raised);
    }

    [Fact]
    public void Mutated_byte_writes_back_through_WriteTo()
    {
        var section = new FftoConfigSection(new byte[] { 0 });
        section.DifficultyLevel = 9;
        var dest = new byte[SaveWorkLayout.FftoConfigSize];
        section.WriteTo(dest);
        Assert.Equal((byte)9, dest[0]);
    }
}

using System.Text;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.GameData.Xml;

namespace TICSaveEditor.Core.Tests.GameData;

public class JobDataXmlReaderTests
{
    private static Stream Xml(string body) =>
        new MemoryStream(Encoding.UTF8.GetBytes(body));

    private const string MinimalSquireBody = """
        <JobTable>
          <Version>1</Version>
          <Entries>
            <Job>
              <Id>1</Id>
              <HPGrowth>11</HPGrowth>
              <HPMultiplier>120</HPMultiplier>
              <MPGrowth>11</MPGrowth>
              <MPMultiplier>105</MPMultiplier>
              <SpeedGrowth>95</SpeedGrowth>
              <SpeedMultiplier>100</SpeedMultiplier>
              <PAGrowth>50</PAGrowth>
              <PAMultiplier>110</PAMultiplier>
              <MAGrowth>48</MAGrowth>
              <MAMultiplier>100</MAMultiplier>
              <Move>4</Move>
              <Jump>3</Jump>
              <CharacterEvasion>10</CharacterEvasion>
            </Job>
          </Entries>
        </JobTable>
        """;

    [Fact]
    public void Read_returns_entry_with_uppercase_HP_PA_MA_mapped_to_PascalCase()
    {
        var reader = new JobDataXmlReader();
        var entries = reader.Read(Xml(MinimalSquireBody));
        var squire = Assert.Single(entries);

        Assert.Equal(1, squire.Id);
        Assert.Equal(11, squire.HpGrowth);
        Assert.Equal(120, squire.HpMultiplier);
        Assert.Equal(11, squire.MpGrowth);
        Assert.Equal(105, squire.MpMultiplier);
        Assert.Equal(95, squire.SpeedGrowth);
        Assert.Equal(100, squire.SpeedMultiplier);
        Assert.Equal(50, squire.PaGrowth);
        Assert.Equal(110, squire.PaMultiplier);
        Assert.Equal(48, squire.MaGrowth);
        Assert.Equal(100, squire.MaMultiplier);
        Assert.Equal(4, squire.Move);
        Assert.Equal(3, squire.Jump);
        Assert.Equal(10, squire.CharacterEvasion);
    }

    [Fact]
    public void Read_silently_skips_known_unused_modloader_elements()
    {
        // Modloader-known fields not consumed in v0.1 (InnateAbilityId, EquippableItems, etc.)
        // are NOT logged — they're known schema, just not in our record yet.
        var body = MinimalSquireBody.Replace(
            "<HPGrowth>11</HPGrowth>",
            "<HPGrowth>11</HPGrowth><InnateAbilityId1>0</InnateAbilityId1><EquippableItems>None</EquippableItems>");

        var logger = new CapturingGameDataLogger();
        var reader = new JobDataXmlReader(logger);
        var entries = reader.Read(Xml(body));

        Assert.Single(entries);
        Assert.DoesNotContain(logger.Warnings, w => w.Contains("InnateAbilityId1"));
        Assert.DoesNotContain(logger.Warnings, w => w.Contains("EquippableItems"));
    }

    [Fact]
    public void Read_warns_on_truly_unknown_element_for_forward_compat()
    {
        // A field NOT in the modloader v1.7.0 schema (e.g., a future version's addition)
        // surfaces as a warning so schema drift is visible.
        var body = MinimalSquireBody.Replace(
            "<HPGrowth>11</HPGrowth>",
            "<HPGrowth>11</HPGrowth><FuturisticNewField>42</FuturisticNewField>");

        var logger = new CapturingGameDataLogger();
        var reader = new JobDataXmlReader(logger);
        reader.Read(Xml(body));

        Assert.Contains(logger.Warnings, w => w.Contains("FuturisticNewField"));
    }

    [Fact]
    public void Read_throws_InvalidDataException_with_id_and_field_name_on_missing_required()
    {
        var body = MinimalSquireBody.Replace("<HPGrowth>11</HPGrowth>", "");
        var reader = new JobDataXmlReader();

        var ex = Assert.Throws<InvalidDataException>(() => reader.Read(Xml(body)));
        Assert.Contains("HpGrowth", ex.Message);
        Assert.Contains("Id=1", ex.Message);
    }

    [Fact]
    public void Read_throws_when_root_or_Entries_is_missing()
    {
        var noEntries = "<JobTable><Version>1</Version></JobTable>";
        var reader = new JobDataXmlReader();
        Assert.Throws<InvalidDataException>(() => reader.Read(Xml(noEntries)));
    }

    [Fact]
    public void Read_ignores_Version_element()
    {
        // Verifies <Version> at root is silently consumed (not warned about, not consumed by entry reader).
        var logger = new CapturingGameDataLogger();
        var reader = new JobDataXmlReader(logger);
        reader.Read(Xml(MinimalSquireBody));

        Assert.DoesNotContain(logger.Warnings, w => w.Contains("Version"));
    }

    [Fact]
    public void Read_returns_all_entries_from_bundled_JobData_xml()
    {
        // Read the actual committed file and verify count + spot-check Squire stats.
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "TICSaveEditor.Core", "Resources", "Modloader", "JobData.xml");
        if (!File.Exists(path))
        {
            return; // file not yet committed in this checkout
        }

        using var stream = File.OpenRead(path);
        var reader = new JobDataXmlReader();
        var entries = reader.Read(stream);

        Assert.True(entries.Count >= 100,
            $"Expected at least 100 Job entries in bundled XML; got {entries.Count}.");

        var squire = entries.FirstOrDefault(e => e.Id == 1);
        Assert.NotNull(squire);
        // From the committed JobData.xml at Id=1 (Squire): HPGrowth=11, HPMultiplier=120, Move=4, Jump=3.
        Assert.Equal(11, squire!.HpGrowth);
        Assert.Equal(120, squire.HpMultiplier);
        Assert.Equal(4, squire.Move);
        Assert.Equal(3, squire.Jump);
    }

    private sealed class CapturingGameDataLogger : IGameDataLogger
    {
        public List<string> Warnings { get; } = new();
        public List<(string Message, Exception? Ex)> Errors { get; } = new();

        public void LogWarning(string message) => Warnings.Add(message);
        public void LogError(string message, Exception? exception = null)
            => Errors.Add((message, exception));
    }
}

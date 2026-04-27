using TICSaveEditor.Core.GameData;

namespace TICSaveEditor.Core.Tests.GameData;

public class GameDataLoaderFallbackTests
{
    [Fact]
    public void LoadWithFallback_null_path_returns_bundled()
    {
        var ctx = new GameDataLoader().LoadWithFallback(null, "en");
        Assert.Equal(GameDataSource.Bundled, ctx.Source);
        Assert.Equal("Squire", ctx.GetJobName(1));
    }

    [Fact]
    public void LoadWithFallback_empty_path_returns_bundled()
    {
        var ctx = new GameDataLoader().LoadWithFallback(string.Empty, "en");
        Assert.Equal(GameDataSource.Bundled, ctx.Source);
    }

    [Fact]
    public void LoadWithFallback_nonexistent_path_returns_bundled_no_throw()
    {
        var bogus = Path.Combine(Path.GetTempPath(), "nonexistent-" + Guid.NewGuid().ToString("N"));
        var ctx = new GameDataLoader().LoadWithFallback(bogus, "en");
        Assert.Equal(GameDataSource.Bundled, ctx.Source);
    }

    [Fact]
    public void LoadWithFallback_invalid_xml_returns_bundled_no_throw()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tic-m7-" + Guid.NewGuid().ToString("N"));
        try
        {
            var enhancedDir = Path.Combine(tempDir, "enhanced");
            Directory.CreateDirectory(enhancedDir);
            File.WriteAllText(Path.Combine(enhancedDir, "JobData.xml"), "<not-valid><JobTable />");

            var ctx = new GameDataLoader().LoadWithFallback(tempDir, "en");
            Assert.Equal(GameDataSource.Bundled, ctx.Source);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void LoadWithFallback_logs_warning_when_falling_back_from_invalid_override()
    {
        var bogus = Path.Combine(Path.GetTempPath(), "nonexistent-" + Guid.NewGuid().ToString("N"));
        var logger = new CapturingLogger();
        var loader = new GameDataLoader(logger);
        loader.LoadWithFallback(bogus, "en");

        Assert.Contains(logger.Warnings, w => w.Contains("user override") && w.Contains("Falling back"));
    }

    [Fact]
    public void LoadUserOverride_throws_on_missing_xml()
    {
        var bogus = Path.Combine(Path.GetTempPath(), "nonexistent-" + Guid.NewGuid().ToString("N"));
        Assert.Throws<FileNotFoundException>(() => new GameDataLoader().LoadUserOverride(bogus, "en"));
    }

    [Fact]
    public void LoadUserOverride_with_valid_xml_returns_UserOverride_source()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "tic-m7-" + Guid.NewGuid().ToString("N"));
        try
        {
            var enhancedDir = Path.Combine(tempDir, "enhanced");
            Directory.CreateDirectory(enhancedDir);
            File.WriteAllText(Path.Combine(enhancedDir, "JobData.xml"), MinimalValidXml);

            var ctx = new GameDataLoader().LoadUserOverride(tempDir, "en");
            Assert.Equal(GameDataSource.UserOverride, ctx.Source);
            Assert.Equal(tempDir, ctx.SourcePath);
            // Override XML has only Job 1; bundled Nex still provides "Squire" name.
            Assert.True(ctx.TryGetJob(1, out var squire));
            Assert.NotNull(squire);
            Assert.Equal("Squire", squire!.Name);
            Assert.Equal(99, squire.HpGrowth); // overridden value
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    private const string MinimalValidXml = """
        <JobTable>
          <Version>1</Version>
          <Entries>
            <Job>
              <Id>1</Id>
              <HPGrowth>99</HPGrowth>
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

    private sealed class CapturingLogger : IGameDataLogger
    {
        public List<string> Warnings { get; } = new();
        public List<(string, Exception?)> Errors { get; } = new();
        public void LogWarning(string message) => Warnings.Add(message);
        public void LogError(string message, Exception? exception = null)
            => Errors.Add((message, exception));
    }
}

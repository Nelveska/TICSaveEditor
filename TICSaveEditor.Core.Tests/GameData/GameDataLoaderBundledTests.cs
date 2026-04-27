using TICSaveEditor.Core.GameData;

namespace TICSaveEditor.Core.Tests.GameData;

public class GameDataLoaderBundledTests
{
    [Fact]
    public void LoadBundled_en_returns_context_with_Bundled_source()
    {
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.Equal(GameDataSource.Bundled, ctx.Source);
        Assert.Equal("en", ctx.Language);
    }

    [Fact]
    public void LoadBundled_en_jobs_count_is_at_least_100()
    {
        // Loose lower bound; spec §11.3 expects 176 but build-pipeline validation is M7.x.
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.True(ctx.Jobs.Count >= 100,
            $"Expected at least 100 jobs in bundled XML; got {ctx.Jobs.Count}.");
    }

    [Fact]
    public void LoadBundled_en_job_id_1_name_is_Squire()
    {
        // Spec §11.3 spot-check.
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.Equal("Squire", ctx.GetJobName(1));
    }

    [Fact]
    public void LoadBundled_en_job_id_1_TryGetJob_returns_populated_record()
    {
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.True(ctx.TryGetJob(1, out var squire));
        Assert.NotNull(squire);
        Assert.Equal(1, squire!.Id);
        Assert.Equal("Squire", squire.Name);
        Assert.Equal(11, squire.HpGrowth);
        Assert.Equal(120, squire.HpMultiplier);
        Assert.Equal(4, squire.Move);
        Assert.Equal(3, squire.Jump);
        Assert.Equal(25, squire.JobCommandId);
    }

    [Fact]
    public void LoadBundled_fr_jobs_have_empty_names_in_M7()
    {
        // Per decisions_m7_partial_language_state.md — only en/Job.json is bundled in M7;
        // fr/ja/de fall back to "Unknown Job (ID n)" until catalogs land in M8.
        var ctx = new GameDataLoader().LoadBundled("fr");
        Assert.Equal("Unknown Job (ID 1)", ctx.GetJobName(1));

        // XML stats are still populated (XML is language-invariant).
        Assert.True(ctx.TryGetJob(1, out var squire));
        Assert.NotNull(squire);
        Assert.Equal(string.Empty, squire!.Name);
        Assert.Equal(11, squire.HpGrowth);
    }

    [Fact]
    public void LoadBundled_unknown_locale_loads_with_xml_only_no_throw()
    {
        // No catalog for "xx" → empty Nex → name fallbacks; XML still loads. No throw.
        var ctx = new GameDataLoader().LoadBundled("xx");
        Assert.Equal("xx", ctx.Language);
        Assert.True(ctx.Jobs.Count >= 100);
        Assert.Equal("Unknown Job (ID 1)", ctx.GetJobName(1));
    }

    [Fact]
    public void LoadBundled_throws_with_clear_error_when_resource_assembly_lacks_JobData()
    {
        // Inject a bare assembly (this test assembly) with no embedded resources.
        // Confirms the InvalidOperationException flags it as a build error.
        var loader = new GameDataLoader(logger: null, resourceAssembly: typeof(GameDataLoaderBundledTests).Assembly);
        var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadBundled("en"));
        Assert.Contains("JobData.xml", ex.Message);
    }
}

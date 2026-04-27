using TICSaveEditor.Core.GameData;

namespace TICSaveEditor.Core.Tests.GameData;

public class BundledGameDataTests
{
    [Fact]
    public void NexLayoutsCommit_is_pinned_to_a_real_sha_not_placeholder()
    {
        Assert.False(BundledGameData.NexLayoutsCommit.StartsWith('<'),
            $"NexLayoutsCommit must be a real SHA; got '{BundledGameData.NexLayoutsCommit}'.");
        Assert.False(string.IsNullOrWhiteSpace(BundledGameData.NexLayoutsCommit));
    }

    [Fact]
    public void Ff16ToolsVersion_is_pinned_not_placeholder()
    {
        Assert.False(BundledGameData.Ff16ToolsVersion.StartsWith('<'),
            $"Ff16ToolsVersion must be a real version; got '{BundledGameData.Ff16ToolsVersion}'.");
        Assert.False(string.IsNullOrWhiteSpace(BundledGameData.Ff16ToolsVersion));
    }

    [Fact]
    public void BundledLanguages_declares_all_four_v0_1_locales()
    {
        // Even though only en/Job.json is committed in M7 (per decisions_m7_partial_language_state.md),
        // the declared list reflects the v0.1 release intent.
        Assert.Contains("en", BundledGameData.BundledLanguages);
        Assert.Contains("fr", BundledGameData.BundledLanguages);
        Assert.Contains("ja", BundledGameData.BundledLanguages);
        Assert.Contains("de", BundledGameData.BundledLanguages);
    }

    [Fact]
    public void ModloaderVersion_matches_v1_7_0()
    {
        // Pinned in M7 to the modloader version Nenkai shipped at JobData.xml capture.
        Assert.Equal("1.7.0", BundledGameData.ModloaderVersion);
    }
}

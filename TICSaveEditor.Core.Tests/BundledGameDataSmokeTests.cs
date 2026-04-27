using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Validation;

namespace TICSaveEditor.Core.Tests;

public class BundledGameDataSmokeTests
{
    [Fact]
    public void BundledLanguages_includes_english()
    {
        Assert.Contains("en", BundledGameData.BundledLanguages);
    }

    [Fact]
    public void ValidationResult_Empty_is_valid_with_no_warnings()
    {
        var result = ValidationResult.Empty;
        Assert.True(result.IsValid);
        Assert.False(result.HasWarnings);
        Assert.Empty(result.Issues);
    }
}

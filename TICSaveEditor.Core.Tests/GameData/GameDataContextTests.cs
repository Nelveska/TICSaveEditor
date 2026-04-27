using TICSaveEditor.Core.GameData;

namespace TICSaveEditor.Core.Tests.GameData;

public class GameDataContextTests
{
    private static GameDataContext NewEmptyContext(string language = "en") =>
        new(
            language: language,
            source: GameDataSource.Bundled,
            sourcePath: "<test>",
            jobs: JobDataTable.Empty,
            items: ItemDataTable.Empty,
            abilities: AbilityDataTable.Empty,
            jobCommands: JobCommandDataTable.Empty,
            statusEffects: StatusEffectDataTable.Empty,
            characterNames: CharacterNameTable.Empty);

    [Fact]
    public void Empty_context_exposes_zero_length_collections_for_every_domain()
    {
        var ctx = NewEmptyContext();
        Assert.Empty(ctx.Jobs);
        Assert.Empty(ctx.Items);
        Assert.Empty(ctx.Abilities);
        Assert.Empty(ctx.JobCommands);
        Assert.Empty(ctx.StatusEffects);
        Assert.Empty(ctx.CharacterNames);
    }

    [Fact]
    public void GetJobName_unknown_id_returns_unknown_pattern()
    {
        var ctx = NewEmptyContext();
        Assert.Equal("Unknown Job (ID 7)", ctx.GetJobName(7));
    }

    [Fact]
    public void GetItemName_unknown_id_returns_unknown_pattern()
    {
        var ctx = NewEmptyContext();
        Assert.Equal("Unknown Item (ID 42)", ctx.GetItemName(42));
    }

    [Fact]
    public void GetAbilityName_GetCommandName_GetStatusEffectName_use_domain_prefix()
    {
        var ctx = NewEmptyContext();
        Assert.Equal("Unknown Ability (ID 1)", ctx.GetAbilityName(1));
        Assert.Equal("Unknown Command (ID 2)", ctx.GetCommandName(2));
        Assert.Equal("Unknown StatusEffect (ID 3)", ctx.GetStatusEffectName(3));
    }

    [Fact]
    public void GetCharacterName_uses_ushort_NameNo_in_fallback()
    {
        var ctx = NewEmptyContext();
        Assert.Equal("Unknown Character (NameNo 100)", ctx.GetCharacterName(100));
    }

    [Fact]
    public void TryGetJob_unknown_id_returns_false_and_null()
    {
        var ctx = NewEmptyContext();
        var ok = ctx.TryGetJob(99, out var info);
        Assert.False(ok);
        Assert.Null(info);
    }

    [Fact]
    public void TryGetJob_known_id_returns_true_and_record_with_value_equality()
    {
        var entry = new JobInfo(
            Id: 1, Name: "Squire", Description: "desc",
            JobTypeId: 0, JobCommandId: 0,
            HpGrowth: 1, HpMultiplier: 100,
            MpGrowth: 1, MpMultiplier: 100,
            SpeedGrowth: 1, SpeedMultiplier: 100,
            PaGrowth: 1, PaMultiplier: 100,
            MaGrowth: 1, MaMultiplier: 100,
            Move: 4, Jump: 3, CharacterEvasion: 5);
        var jobs = new JobDataTable(new[] { entry });
        var ctx = new GameDataContext("en", GameDataSource.Bundled, "<test>",
            jobs, ItemDataTable.Empty, AbilityDataTable.Empty,
            JobCommandDataTable.Empty, StatusEffectDataTable.Empty, CharacterNameTable.Empty);

        Assert.True(ctx.TryGetJob(1, out var got));
        Assert.NotNull(got);
        Assert.Equal(entry, got);
    }

    [Fact]
    public void Language_Source_SourcePath_round_trip_through_context()
    {
        var ctx = new GameDataContext(
            language: "fr",
            source: GameDataSource.UserOverride,
            sourcePath: "C:/some/path",
            jobs: JobDataTable.Empty,
            items: ItemDataTable.Empty,
            abilities: AbilityDataTable.Empty,
            jobCommands: JobCommandDataTable.Empty,
            statusEffects: StatusEffectDataTable.Empty,
            characterNames: CharacterNameTable.Empty);

        Assert.Equal("fr", ctx.Language);
        Assert.Equal(GameDataSource.UserOverride, ctx.Source);
        Assert.Equal("C:/some/path", ctx.SourcePath);
    }
}

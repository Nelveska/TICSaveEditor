using TICSaveEditor.Core.GameData;

namespace TICSaveEditor.Core.Tests.GameData;

public class GameDataLoaderAllTablesTests
{
    [Fact]
    public void LoadBundled_en_populates_jobs_items_abilities_jobCommands_statusEffects_charaNames()
    {
        var ctx = new GameDataLoader().LoadBundled("en");

        Assert.NotEmpty(ctx.Jobs);
        Assert.NotEmpty(ctx.Items);
        Assert.NotEmpty(ctx.Abilities);
        Assert.NotEmpty(ctx.JobCommands);
        Assert.NotEmpty(ctx.StatusEffects);
        Assert.NotEmpty(ctx.CharacterNames);
    }

    [Fact]
    public void GetItemName_id_1_returns_dagger_in_english()
    {
        // Item Id=1 is Dagger per the modloader XML comment "Dagger / たかー / dague / dolch".
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.Equal("Dagger", ctx.GetItemName(1));
    }

    [Fact]
    public void GetAbilityName_id_1_returns_cure_in_english()
    {
        // Ability Id=1 is Cure per the modloader XML comment.
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.Equal("Cure", ctx.GetAbilityName(1));
    }

    [Fact]
    public void GetCommandName_id_25_returns_squires_command_name()
    {
        // Job Id=1 (Squire) → JobCommandId 25 → JobCommand entry has a non-empty Name.
        var ctx = new GameDataLoader().LoadBundled("en");
        var name = ctx.GetCommandName(25);
        Assert.False(string.IsNullOrEmpty(name));
        Assert.False(name.StartsWith("Unknown"),
            $"Expected real JobCommand name for Id=25, got '{name}'.");
    }

    [Fact]
    public void GetStatusEffectName_id_3_returns_KO()
    {
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.Equal("KO", ctx.GetStatusEffectName(3));
    }

    [Fact]
    public void GetCharacterName_nameNo_1_returns_Ramza()
    {
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.Equal("Ramza", ctx.GetCharacterName(1));
    }

    [Fact]
    public void TryGetItem_id_1_dagger_has_Knife_category_and_price_100()
    {
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.True(ctx.TryGetItem(1, out var dagger));
        Assert.NotNull(dagger);
        Assert.Equal("Dagger", dagger!.Name);
        Assert.Equal("Knife", dagger.ItemCategory);
        Assert.Equal(100, dagger.Price);
        Assert.Equal((byte)1, dagger.RequiredLevel);
    }

    [Fact]
    public void TryGetAbility_id_1_cure_has_normal_type_and_chance_90()
    {
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.True(ctx.TryGetAbility(1, out var cure));
        Assert.NotNull(cure);
        Assert.Equal("Cure", cure!.Name);
        Assert.Equal("Normal", cure.AbilityType);
        Assert.Equal((byte)90, cure.ChanceToLearn);
        Assert.True(cure.JpCost > 0, $"Expected non-zero JpCost for Cure; got {cure.JpCost}.");
    }

    [Fact]
    public void TryGetCharacterName_nameNo_1_isGeneric_false()
    {
        var ctx = new GameDataLoader().LoadBundled("en");
        Assert.True(ctx.TryGetCharacterName(1, out var ramza));
        Assert.NotNull(ramza);
        Assert.Equal("Ramza", ramza!.Name);
        Assert.False(ramza.IsGeneric, "Ramza is a named character, not a generic template.");
    }

    [Fact]
    public void LoadBundled_fr_falls_back_for_all_six_tables_in_M7x()
    {
        // Per decisions_m7_partial_language_state.md: only en/*.json is committed in M7/M7.x.
        // Non-en LoadBundled produces empty Nex catalogs across every table; XML-derived stats
        // still load (XML is language-invariant). GetX uses the unknown fallback for names.
        var ctx = new GameDataLoader().LoadBundled("fr");

        Assert.Equal("Unknown Job (ID 1)", ctx.GetJobName(1));
        Assert.Equal("Unknown Item (ID 1)", ctx.GetItemName(1));
        Assert.Equal("Unknown Ability (ID 1)", ctx.GetAbilityName(1));
        // JobCommand/StatusEffect/CharaName have no XML, so collections are also empty in non-en.
        Assert.Empty(ctx.JobCommands);
        Assert.Empty(ctx.StatusEffects);
        Assert.Empty(ctx.CharacterNames);

        // XML stats still populated for Job/Item/Ability:
        Assert.True(ctx.TryGetJob(1, out var job));
        Assert.NotNull(job);
        Assert.Equal(11, job!.HpGrowth);
    }

    [Fact]
    public void LoadWithFallback_null_path_returns_bundled_with_all_tables()
    {
        var ctx = new GameDataLoader().LoadWithFallback(null, "en");
        Assert.Equal(GameDataSource.Bundled, ctx.Source);
        Assert.NotEmpty(ctx.Jobs);
        Assert.NotEmpty(ctx.Items);
        Assert.NotEmpty(ctx.Abilities);
    }
}

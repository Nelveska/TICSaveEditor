using TICSaveEditor.Core.GameData;

namespace TICSaveEditor.Core.Tests.GameData;

public class JobInfoIdentityTests
{
    private static JobInfo Make(int id, string name = "Squire") =>
        new(id, name, "desc", 0, 0,
            HpGrowth: 1, HpMultiplier: 100,
            MpGrowth: 1, MpMultiplier: 100,
            SpeedGrowth: 1, SpeedMultiplier: 100,
            PaGrowth: 1, PaMultiplier: 100,
            MaGrowth: 1, MaMultiplier: 100,
            Move: 4, Jump: 3, CharacterEvasion: 5);

    [Fact]
    public void JobInfo_with_same_fields_compare_equal()
    {
        Assert.Equal(Make(1), Make(1));
    }

    [Fact]
    public void JobInfo_with_same_fields_share_hash_code()
    {
        Assert.Equal(Make(1).GetHashCode(), Make(1).GetHashCode());
    }

    [Fact]
    public void JobInfo_with_different_id_compares_unequal()
    {
        Assert.NotEqual(Make(1), Make(2));
    }
}

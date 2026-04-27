using System.ComponentModel;
using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Sections;

namespace TICSaveEditor.Core.Tests.Sections;

public class BattleSectionTests
{
    private static byte[] BlankBattleBytes() => new byte[SaveWorkLayout.BattleSize];

    [Fact]
    public void Units_collection_has_exactly_54_entries()
    {
        var battle = new BattleSection(BlankBattleBytes());
        Assert.Equal(54, battle.Units.Count);
    }

    [Fact]
    public void Units_indexer_returns_stable_reference()
    {
        var battle = new BattleSection(BlankBattleBytes());
        var first = battle.Units[7];
        var second = battle.Units[7];
        Assert.Same(first, second);
    }

    [Fact]
    public void Random_bytes_round_trip_byte_identical()
    {
        var rng = new Random(2026);
        var bytes = new byte[SaveWorkLayout.BattleSize];
        rng.NextBytes(bytes);
        var pristine = bytes.ToArray();

        var battle = new BattleSection(bytes);

        var output = new byte[SaveWorkLayout.BattleSize];
        battle.WriteTo(output);

        Assert.Equal(pristine, output);
    }

    [Fact]
    public void Mutation_to_unit_propagates_to_WriteTo_output()
    {
        var battle = new BattleSection(BlankBattleBytes());
        battle.Units[3].Level = 42;

        var output = new byte[SaveWorkLayout.BattleSize];
        battle.WriteTo(output);

        // Unit 3 starts at offset 3 * 600 = 1800; Level field is at offset 0x1D within the unit.
        Assert.Equal(42, output[3 * UnitSaveData.Size + 0x1D]);
    }

    [Fact]
    public void Unit_mutation_fires_PropertyChanged_for_Units()
    {
        var battle = new BattleSection(BlankBattleBytes());
        var hits = 0;
        battle.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BattleSection.Units)) hits++;
        };

        battle.Units[0].Level = 5;

        Assert.Equal(1, hits);
    }

    [Fact]
    public void Multiple_unit_mutations_fire_one_event_each()
    {
        var battle = new BattleSection(BlankBattleBytes());
        var hits = 0;
        battle.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BattleSection.Units)) hits++;
        };

        battle.Units[0].Level = 10;
        battle.Units[5].Resist = 1;
        battle.Units[20].Exp = 50;

        Assert.Equal(3, hits);
    }

    [Fact]
    public void Trailing_decomposition_total_length_is_4280_bytes()
    {
        var battle = new BattleSection(BlankBattleBytes());
        var total = battle.PartyItemRaw.Length
                  + battle.ShopItemRaw.Length
                  + battle.FindItemRaw.Length
                  + EventWork.ByteLength
                  + battle.BattleSortRaw.Length;
        Assert.Equal(4280, total);
    }
}

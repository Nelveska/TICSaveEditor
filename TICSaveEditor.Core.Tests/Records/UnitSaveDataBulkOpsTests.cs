using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataBulkOpsTests
{
    [Fact]
    public void LearnAllAbilities_sets_every_AbilityFlags_byte_to_0xFF()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.LearnAllAbilities();

        var output = new byte[600];
        unit.WriteTo(output);
        for (int i = 0x32; i < 0x32 + 22 * 3; i++)
            Assert.Equal(0xFF, output[i]);
    }

    [Fact]
    public void ForgetAllAbilities_clears_every_AbilityFlags_byte()
    {
        var bytes = new byte[600];
        for (int i = 0x32; i < 0x32 + 22 * 3; i++) bytes[i] = 0xFF;
        var unit = new UnitSaveData(bytes);
        unit.ForgetAllAbilities();

        var output = new byte[600];
        unit.WriteTo(output);
        for (int i = 0x32; i < 0x32 + 22 * 3; i++)
            Assert.Equal(0x00, output[i]);
    }

    [Fact]
    public void LearnAllAbilitiesForJob_only_affects_one_job()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.LearnAllAbilitiesForJob(3); // Archer

        var output = new byte[600];
        unit.WriteTo(output);

        // Archer = ability_flag[3] at offset 0x32 + 3*3 = 0x3B..0x3D
        Assert.Equal(0xFF, output[0x3B]);
        Assert.Equal(0xFF, output[0x3C]);
        Assert.Equal(0xFF, output[0x3D]);

        // All others zero.
        for (int i = 0x32; i < 0x32 + 22 * 3; i++)
        {
            if (i >= 0x3B && i <= 0x3D) continue;
            Assert.Equal(0x00, output[i]);
        }
    }

    [Fact]
    public void ForgetAllAbilitiesForJob_only_affects_one_job()
    {
        var bytes = new byte[600];
        for (int i = 0x32; i < 0x32 + 22 * 3; i++) bytes[i] = 0xFF;
        var unit = new UnitSaveData(bytes);
        unit.ForgetAllAbilitiesForJob(5); // White Mage at 0x41..0x43

        var output = new byte[600];
        unit.WriteTo(output);
        Assert.Equal(0x00, output[0x41]);
        Assert.Equal(0x00, output[0x42]);
        Assert.Equal(0x00, output[0x43]);

        for (int i = 0x32; i < 0x32 + 22 * 3; i++)
        {
            if (i >= 0x41 && i <= 0x43) continue;
            Assert.Equal(0xFF, output[i]);
        }
    }

    [Fact]
    public void MaxAllJobPoints_sets_every_slot_to_ushort_max()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.MaxAllJobPoints();
        for (int i = 0; i < 23; i++) Assert.Equal(ushort.MaxValue, unit.GetJobPoint(i));
    }

    [Fact]
    public void ZeroAllJobPoints_clears_every_slot()
    {
        var unit = new UnitSaveData(new byte[600]);
        for (int i = 0; i < 23; i++) unit.SetJobPoint(i, 1234);
        unit.ZeroAllJobPoints();
        for (int i = 0; i < 23; i++) Assert.Equal(0, unit.GetJobPoint(i));
    }

    [Fact]
    public void MaxAllJobPoints_raises_PropertyChanged_for_JobPoints_exactly_once()
    {
        var unit = new UnitSaveData(new byte[600]);
        var hits = 0;
        unit.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(UnitSaveData.JobPoints)) hits++; };

        unit.MaxAllJobPoints();

        Assert.Equal(1, hits);
    }

    [Fact]
    public void LearnAllAbilities_raises_PropertyChanged_for_AbilityFlags_exactly_once()
    {
        var unit = new UnitSaveData(new byte[600]);
        var hits = 0;
        unit.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(UnitSaveData.AbilityFlags)) hits++; };

        unit.LearnAllAbilities();

        Assert.Equal(1, hits);
    }
}

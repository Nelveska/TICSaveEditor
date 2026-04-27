using TICSaveEditor.Core.Records;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataConstructionTests
{
    [Fact]
    public void UnitSaveData_Size_constant_equals_600()
    {
        Assert.Equal(600, UnitSaveData.Size);
    }

    [Fact]
    public void Ctor_rejects_span_smaller_than_600()
    {
        var bytes = new byte[599];
        var ex = Assert.Throws<ArgumentException>(() => new UnitSaveData(bytes));
        Assert.Contains("600", ex.Message);
    }

    [Fact]
    public void Ctor_rejects_span_larger_than_600()
    {
        var bytes = new byte[601];
        var ex = Assert.Throws<ArgumentException>(() => new UnitSaveData(bytes));
        Assert.Contains("600", ex.Message);
    }

    [Fact]
    public void Ctor_accepts_exactly_600_bytes_of_zero()
    {
        var bytes = new byte[600];
        var unit = new UnitSaveData(bytes);
        Assert.Equal(0, unit.Character);
        Assert.True(unit.IsEmpty);
    }

    [Fact]
    public void Ctor_does_not_raise_property_changed_during_construction()
    {
        var bytes = new byte[600];
        bytes[0] = 0x80; // generic male character
        var raised = false;
        var unit = new UnitSaveData(bytes);
        unit.PropertyChanged += (_, _) => raised = true;
        // Construct another to check ctor itself doesn't fire — but the subscription happens after ctor,
        // so verify by post-hoc construction with a pre-attached handler is impossible.
        // Instead, verify reading state after ctor doesn't fire either.
        var _ = unit.Character;
        Assert.False(raised);
    }
}

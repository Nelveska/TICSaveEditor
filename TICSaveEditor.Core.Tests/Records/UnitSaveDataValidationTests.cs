using TICSaveEditor.Core.Records;
using TICSaveEditor.Core.Validation;

namespace TICSaveEditor.Core.Tests.Records;

public class UnitSaveDataValidationTests
{
    private static UnitSaveData NewValidUnit()
    {
        var unit = new UnitSaveData(new byte[600]);
        unit.Level = 1;
        return unit;
    }

    [Fact]
    public void Validate_returns_empty_for_blank_unit_with_legal_level_1()
    {
        var unit = NewValidUnit();
        var result = unit.Validate();
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_blank_unit_returns_Level_error_when_level_zero()
    {
        var unit = new UnitSaveData(new byte[600]);
        var result = unit.Validate();
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.FieldName == nameof(UnitSaveData.Level));
    }

    [Fact]
    public void Validate_returns_error_when_Level_is_zero()
    {
        var unit = NewValidUnit();
        unit.Level = 0;
        var result = unit.Validate();
        Assert.Contains(result.Issues, i => i.FieldName == nameof(UnitSaveData.Level) && i.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validate_returns_error_when_Level_is_100()
    {
        var unit = NewValidUnit();
        unit.Level = 100;
        Assert.Contains(unit.Validate().Issues, i => i.FieldName == nameof(UnitSaveData.Level));
    }

    [Fact]
    public void Validate_returns_error_when_StartBcp_is_101()
    {
        var unit = NewValidUnit();
        unit.StartBcp = 101;
        Assert.Contains(unit.Validate().Issues, i => i.FieldName == nameof(UnitSaveData.StartBcp));
    }

    [Fact]
    public void Validate_returns_error_when_StartFaith_is_101()
    {
        var unit = NewValidUnit();
        unit.StartFaith = 101;
        Assert.Contains(unit.Validate().Issues, i => i.FieldName == nameof(UnitSaveData.StartFaith));
    }

    [Fact]
    public void Validate_returns_error_when_Zodiac_high_nibble_is_12()
    {
        // Zodiac sign lives in the high nibble of byte 0x06 (sign << 4).
        // 0xC0 = high-nibble 12 = invalid sign (signs are 0..11). Low nibble is preserved.
        var unit = NewValidUnit();
        unit.Zodiac = 0xC0;
        Assert.Contains(unit.Validate().Issues, i => i.FieldName == nameof(UnitSaveData.Zodiac));
    }

    [Fact]
    public void Validate_does_not_error_on_high_zodiac_byte_when_sign_is_valid()
    {
        // Byte 0x81: sign = 8 (Sagittarius), low-nibble flag = 1. Both valid.
        // This is the value pattern observed in our real-save fixtures.
        var unit = NewValidUnit();
        unit.Zodiac = 0x81;
        Assert.DoesNotContain(unit.Validate().Issues, i => i.FieldName == nameof(UnitSaveData.Zodiac));
    }

    [Fact]
    public void Validate_returns_error_when_CurrentEquipSetNumber_is_3()
    {
        var unit = NewValidUnit();
        unit.CurrentEquipSetNumber = 3;
        Assert.Contains(unit.Validate().Issues, i => i.FieldName == nameof(UnitSaveData.CurrentEquipSetNumber));
    }
}

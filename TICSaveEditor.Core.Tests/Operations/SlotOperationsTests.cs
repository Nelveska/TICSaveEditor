using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Operations;

public class SlotOperationsTests
{
    private static ManualSaveFile NewManualSaveFile()
    {
        var slots = new SaveSlot[ManualSaveFile.SlotCount];
        for (int i = 0; i < ManualSaveFile.SlotCount; i++)
        {
            slots[i] = new SaveSlot(i, new SaveWork(new byte[SaveWork.Size]));
        }
        return new ManualSaveFile(
            version: 0x10,
            storedChecksum: 0u,
            formatDiscriminator: 0ul,
            sourcePath: "<test>",
            originalPngEnvelope: null,
            originalUnwrappedPayload: null,
            slots: slots);
    }

    private static void PopulateUnit(ManualSaveFile file, int slotIdx, int unitIdx, byte character)
    {
        var unit = file.Slots[slotIdx].SaveWork!.Battle.Units[unitIdx];
        unit.Character = character;
        unit.Job = 1;
        unit.Level = 50;
    }

    [Fact]
    public void CopyCharacter_returns_error_on_slot_out_of_range()
    {
        var file = NewManualSaveFile();
        var result = SlotOperations.CopyCharacter(file, sourceSlot: 99, 0, 0, 0);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, i => i.Severity == OperationSeverity.Error);
    }

    [Fact]
    public void CopyCharacter_returns_error_on_unit_out_of_range()
    {
        var file = NewManualSaveFile();
        var result = SlotOperations.CopyCharacter(file, 0, sourceUnitIndex: 54, 0, 0);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, i => i.Severity == OperationSeverity.Error);
    }

    [Fact]
    public void CopyCharacter_returns_error_when_source_unit_is_empty()
    {
        var file = NewManualSaveFile();
        var result = SlotOperations.CopyCharacter(file, 0, 0, 0, 1);
        Assert.False(result.Succeeded);
        Assert.Contains(result.Issues, i => i.Severity == OperationSeverity.Error);
    }

    [Fact]
    public void CopyCharacter_succeeds_when_source_populated_and_dest_empty()
    {
        var file = NewManualSaveFile();
        PopulateUnit(file, 0, 0, character: 5);

        var result = SlotOperations.CopyCharacter(file, 0, 0, 0, 1);
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.UnitsAffected);

        var copy = file.Slots[0].SaveWork!.Battle.Units[1];
        Assert.Equal((byte)5, copy.Character);
        Assert.Equal((byte)50, copy.Level);
    }

    [Fact]
    public void CopyCharacter_warns_when_dest_non_empty_but_still_succeeds()
    {
        var file = NewManualSaveFile();
        PopulateUnit(file, 0, 0, character: 5);
        PopulateUnit(file, 0, 1, character: 9);

        var result = SlotOperations.CopyCharacter(file, 0, 0, 0, 1);
        Assert.True(result.Succeeded);
        Assert.Contains(result.Issues, i => i.Severity == OperationSeverity.Warning);
        // Destination overwritten by source.
        Assert.Equal((byte)5, file.Slots[0].SaveWork!.Battle.Units[1].Character);
    }

    [Fact]
    public void DuplicateCharacter_behaves_identically_to_CopyCharacter()
    {
        var file1 = NewManualSaveFile();
        var file2 = NewManualSaveFile();
        PopulateUnit(file1, 0, 0, character: 7);
        PopulateUnit(file2, 0, 0, character: 7);

        var copyResult = SlotOperations.CopyCharacter(file1, 0, 0, 0, 1);
        var dupResult = SlotOperations.DuplicateCharacter(file2, 0, 0, 0, 1);

        Assert.Equal(copyResult.Succeeded, dupResult.Succeeded);
        Assert.Equal(copyResult.UnitsAffected, dupResult.UnitsAffected);
        Assert.Equal(
            file1.Slots[0].SaveWork!.Battle.Units[1].Character,
            file2.Slots[0].SaveWork!.Battle.Units[1].Character);
    }

    [Fact]
    public void SwapCharacter_swaps_two_populated_units()
    {
        var file = NewManualSaveFile();
        PopulateUnit(file, 0, 0, character: 5);
        PopulateUnit(file, 0, 1, character: 9);

        var result = SlotOperations.SwapCharacter(file, 0, 0, 0, 1);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.UnitsAffected);
        Assert.Equal((byte)9, file.Slots[0].SaveWork!.Battle.Units[0].Character);
        Assert.Equal((byte)5, file.Slots[0].SaveWork!.Battle.Units[1].Character);
    }

    [Fact]
    public void SwapCharacter_does_not_error_on_empty_source()
    {
        var file = NewManualSaveFile();
        PopulateUnit(file, 0, 1, character: 9);

        // Source slot 0 unit 0 is empty; spec allows this for Swap (no-op effect).
        var result = SlotOperations.SwapCharacter(file, 0, 0, 0, 1);

        Assert.True(result.Succeeded);
        // After swap: unit 0 has character 9, unit 1 is empty.
        Assert.Equal((byte)9, file.Slots[0].SaveWork!.Battle.Units[0].Character);
        Assert.True(file.Slots[0].SaveWork!.Battle.Units[1].IsEmpty);
    }

    [Fact]
    public void Operations_throw_on_null_file()
    {
        Assert.Throws<ArgumentNullException>(() => SlotOperations.CopyCharacter(null!, 0, 0, 0, 0));
        Assert.Throws<ArgumentNullException>(() => SlotOperations.DuplicateCharacter(null!, 0, 0, 0, 0));
        Assert.Throws<ArgumentNullException>(() => SlotOperations.SwapCharacter(null!, 0, 0, 0, 0));
    }
}

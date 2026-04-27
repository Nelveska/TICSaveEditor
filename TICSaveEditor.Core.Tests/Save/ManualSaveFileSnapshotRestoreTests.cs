using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Tests.Save;

public class ManualSaveFileSnapshotRestoreTests
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

    [Fact]
    public void CreateSnapshot_returns_byte_array_full_payload_size()
    {
        var file = NewManualSaveFile();
        var snapshot = file.CreateSnapshot();
        var bytes = Assert.IsType<byte[]>(snapshot);
        // 0x10 outer header + 50 × SaveWork.Size
        Assert.Equal(0x10 + 50 * SaveWork.Size, bytes.Length);
    }

    [Fact]
    public void RestoreFromSnapshot_reverts_a_per_slot_mutation_only_in_that_slot()
    {
        var file = NewManualSaveFile();
        var snapshot = file.CreateSnapshot();

        // Mutate slot 0 only.
        file.Slots[0].SaveWork!.Card.Title = "Mutated";
        var titleAfterMutation = file.Slots[0].SaveWork!.Card.Title;
        Assert.Equal("Mutated", titleAfterMutation);

        file.RestoreFromSnapshot(snapshot);

        // Slot 0 reverted; other slots untouched (they were never mutated).
        Assert.NotEqual("Mutated", file.Slots[0].SaveWork!.Card.Title);
    }

    [Fact]
    public void RestoreFromSnapshot_throws_on_wrong_length()
    {
        var file = NewManualSaveFile();
        Assert.Throws<ArgumentException>(() => file.RestoreFromSnapshot(new byte[100]));
    }

    [Fact]
    public void SuspendNotifications_composes_50_slot_disposables()
    {
        var file = NewManualSaveFile();
        // Smoke: opening + disposing the suspend scope must not throw.
        using (file.SuspendNotifications())
        {
            // Mutate inside suspend; events queue per-section.
            file.Slots[0].SaveWork!.Card.Title = "X";
        }
    }
}

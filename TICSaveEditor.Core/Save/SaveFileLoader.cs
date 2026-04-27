using System.Buffers.Binary;

namespace TICSaveEditor.Core.Save;

public static class SaveFileLoader
{
    private const int OuterHeaderSize = 0x10;
    private const ulong ResumeDiscriminator = 0x10ul;
    private const int FftiSaveTypeFileOffset = 0x1C;

    public static SaveFile Load(string path)
    {
        var backup = new SaveDirectoryBackup(new BackupOptions());
        backup.BackupSiblings(path);
        var bytes = File.ReadAllBytes(path);
        return Load(bytes, path);
    }

    public static SaveFile Load(byte[] bytes, string sourcePath)
    {
        byte[]? originalPngEnvelope = null;
        byte[]? originalUnwrappedPayload = null;
        var payload = bytes;
        if (sourcePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            originalPngEnvelope = bytes;
            var ffToChunk = PngEnvelope.Extract(bytes);
            payload = UmifContainer.Unpack(ffToChunk);
            originalUnwrappedPayload = payload;
        }

        if (payload.Length < OuterHeaderSize)
        {
            throw new InvalidDataException(
                $"Save payload too small: {payload.Length} bytes (need at least {OuterHeaderSize}).");
        }

        var version = (int)BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0x00, 4));
        var storedChecksum = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0x04, 4));
        var discriminator = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(0x08, 8));

        if (discriminator == ResumeDiscriminator)
        {
            return LoadResume(version, storedChecksum, discriminator, sourcePath,
                originalPngEnvelope, originalUnwrappedPayload, payload);
        }
        return LoadManual(version, storedChecksum, discriminator, sourcePath,
            originalPngEnvelope, originalUnwrappedPayload, payload);
    }

    private static SaveFile LoadResume(
        int version, uint crc, ulong discriminator, string sourcePath,
        byte[]? originalPng, byte[]? originalUnwrappedPayload, byte[] payload)
    {
        if (payload.Length < OuterHeaderSize + FftiHeader.KnownPrefixSize)
        {
            throw new InvalidDataException("Resume save payload too small for FFTI prefix.");
        }
        var saveType = BinaryPrimitives.ReadUInt32LittleEndian(
            payload.AsSpan(FftiSaveTypeFileOffset, 4));

        return saveType switch
        {
            0u => LoadResumeWorld(version, crc, discriminator, sourcePath,
                originalPng, originalUnwrappedPayload, payload),
            1u => LoadResumeBattle(version, crc, discriminator, sourcePath,
                originalPng, originalUnwrappedPayload, payload),
            _ => throw new InvalidDataException($"Unknown FFTI SaveType: {saveType}"),
        };
    }

    private static ResumeWorldSaveFile LoadResumeWorld(
        int version, uint crc, ulong discriminator, string sourcePath,
        byte[]? originalPng, byte[]? originalUnwrappedPayload, byte[] payload)
    {
        var fftiSize = payload.Length - OuterHeaderSize - SaveWork.Size;
        if (fftiSize < FftiHeader.KnownPrefixSize)
        {
            throw new InvalidDataException(
                $"Resume world save: FFTI region too small ({fftiSize} bytes).");
        }
        var ffti = new FftiHeader(payload.AsSpan(OuterHeaderSize, fftiSize));
        var saveWork = new SaveWork(payload.AsSpan(OuterHeaderSize + fftiSize, SaveWork.Size));
        return new ResumeWorldSaveFile(
            version, crc, discriminator, sourcePath,
            originalPng, originalUnwrappedPayload, ffti, saveWork);
    }

    private static ResumeBattleSaveFile LoadResumeBattle(
        int version, uint crc, ulong discriminator, string sourcePath,
        byte[]? originalPng, byte[]? originalUnwrappedPayload, byte[] payload)
    {
        var ffti = new FftiHeader(
            payload.AsSpan(OuterHeaderSize, FftiHeader.KnownPrefixSize));
        var rawPayloadStart = OuterHeaderSize + FftiHeader.KnownPrefixSize;
        var rawPayload = payload.AsSpan(rawPayloadStart).ToArray();
        return new ResumeBattleSaveFile(
            version, crc, discriminator, sourcePath,
            originalPng, originalUnwrappedPayload, ffti, rawPayload);
    }

    private static ManualSaveFile LoadManual(
        int version, uint crc, ulong discriminator, string sourcePath,
        byte[]? originalPng, byte[]? originalUnwrappedPayload, byte[] payload)
    {
        var expectedSize = OuterHeaderSize + ManualSaveFile.SlotCount * SaveWork.Size;
        if (payload.Length < expectedSize)
        {
            throw new InvalidDataException(
                $"Manual save too small: {payload.Length} bytes (need at least {expectedSize}).");
        }

        var slots = new SaveSlot[ManualSaveFile.SlotCount];
        for (var i = 0; i < ManualSaveFile.SlotCount; i++)
        {
            var sw = new SaveWork(
                payload.AsSpan(OuterHeaderSize + i * SaveWork.Size, SaveWork.Size));
            slots[i] = new SaveSlot(i, sw);
        }
        return new ManualSaveFile(
            version, crc, discriminator, sourcePath,
            originalPng, originalUnwrappedPayload, slots);
    }
}

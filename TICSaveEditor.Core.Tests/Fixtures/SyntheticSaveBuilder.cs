using System.Buffers.Binary;
using System.Text;
using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Tests.Fixtures;

internal static class SyntheticSaveBuilder
{
    public const uint Slot0Magic = 0xDEADBEEFu;
    public const int ResumeWorldFftiSize = 0x200;
    public const string Slot0Title = "Slot 0 Title";
    public const byte Slot0DifficultyLevel = 2;

    private const int OuterHeaderSize = 0x10;

    public static byte[] BuildManualSaveRaw()
    {
        var totalSize = OuterHeaderSize + ManualSaveFile.SlotCount * SaveWork.Size;
        var buffer = new byte[totalSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x00, 4), 0x10u);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(0x08, 8), 0x00ul);

        // Slot 0 starts at file offset OuterHeaderSize.
        var slot0Start = OuterHeaderSize;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(slot0Start, 4), Slot0Magic);
        WriteSlotCardTitle(buffer, slotIndex: 0, Slot0Title);
        WriteSlotDifficulty(buffer, slotIndex: 0, Slot0DifficultyLevel);

        PatchCrc(buffer);
        return buffer;
    }

    public static byte[] BuildManualSavePng() => WrapInPng(BuildManualSaveRaw());

    public static byte[] BuildResumeWorldSaveRaw()
    {
        var totalSize = OuterHeaderSize + ResumeWorldFftiSize + SaveWork.Size;
        var buffer = new byte[totalSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x00, 4), 0x10u);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(0x08, 8), 0x10ul);

        WriteFfti(
            buffer.AsSpan(OuterHeaderSize, ResumeWorldFftiSize),
            saveType: 0u,
            sizeFromFftiToEof: (uint)(ResumeWorldFftiSize + SaveWork.Size));

        var rng = new Random(42);
        rng.NextBytes(buffer.AsSpan(OuterHeaderSize + 0x154, ResumeWorldFftiSize - 0x154));

        PatchCrc(buffer);
        return buffer;
    }

    public static byte[] BuildResumeWorldSavePng() => WrapInPng(BuildResumeWorldSaveRaw());

    public static byte[] BuildResumeBattleSaveRaw(int rawPayloadSize = 1024)
    {
        var totalSize = OuterHeaderSize + FftiHeader.KnownPrefixSize + rawPayloadSize;
        var buffer = new byte[totalSize];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x00, 4), 0x10u);
        BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(0x08, 8), 0x10ul);

        WriteFfti(
            buffer.AsSpan(OuterHeaderSize, FftiHeader.KnownPrefixSize),
            saveType: 1u,
            sizeFromFftiToEof: (uint)(FftiHeader.KnownPrefixSize + rawPayloadSize));

        var rng = new Random(123);
        rng.NextBytes(buffer.AsSpan(OuterHeaderSize + FftiHeader.KnownPrefixSize));

        PatchCrc(buffer);
        return buffer;
    }

    public static byte[] BuildResumeBattleSavePng(int rawPayloadSize = 1024)
        => WrapInPng(BuildResumeBattleSaveRaw(rawPayloadSize));

    private static void WriteSlotCardTitle(byte[] buffer, int slotIndex, string title)
    {
        var slotStart = OuterHeaderSize + slotIndex * SaveWork.Size;
        var titleStart = slotStart + SaveWorkLayout.CardOffset + 0x04;
        var encoded = Encoding.ASCII.GetBytes(title);
        var len = Math.Min(encoded.Length, 0x40 - 1);
        Buffer.BlockCopy(encoded, 0, buffer, titleStart, len);
        // Trailing null already present (buffer is zero-initialized).
    }

    private static void WriteSlotDifficulty(byte[] buffer, int slotIndex, byte difficulty)
    {
        var slotStart = OuterHeaderSize + slotIndex * SaveWork.Size;
        buffer[slotStart + SaveWorkLayout.FftoConfigOffset] = difficulty;
    }

    private static void WriteFfti(Span<byte> dest, uint saveType, uint sizeFromFftiToEof)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(0x00, 4), FftiHeader.MagicValue);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(0x04, 4), (uint)FftiHeader.KnownPrefixSize);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(0x08, 4), sizeFromFftiToEof);
        BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(0x0C, 4), saveType);
        BinaryPrimitives.WriteUInt32LittleEndian(
            dest.Slice(0x10, 4),
            saveType == 0 ? 0x2B4238u : 0x3485E10u);
        BinaryPrimitives.WriteInt64LittleEndian(dest.Slice(0x38, 8), 1745539200L);
    }

    private static void PatchCrc(byte[] buffer)
    {
        var crc = Crc32.Compute(buffer.AsSpan(OuterHeaderSize));
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(0x04, 4), crc);
    }

    private static byte[] WrapInPng(byte[] payload)
    {
        // Real game saves wrap the payload in a UMIF container inside the ffTo chunk.
        // Synthetic PNGs match that layering so the load path is exercised end-to-end.
        var umif = UmifContainer.Pack(payload);

        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        WriteChunk(ms, "IHDR", new byte[13]);
        WriteChunk(ms, "ffTo", umif);
        WriteChunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static void WriteChunk(Stream s, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
        s.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        s.Write(typeBytes);
        s.Write(data);

        var crcInput = new byte[4 + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcInput, 0, 4);
        Buffer.BlockCopy(data, 0, crcInput, 4, data.Length);
        var crc = Crc32.Compute(crcInput);
        Span<byte> crcSpan = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcSpan, crc);
        s.Write(crcSpan);
    }
}

using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Save;

internal static class PngEnvelope
{
    private static ReadOnlySpan<byte> Signature
        => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    private static ReadOnlySpan<byte> FftoType
        => new byte[] { 0x66, 0x66, 0x54, 0x6F };

    public static byte[] Extract(byte[] pngBytes)
    {
        ValidateSignature(pngBytes);
        var (dataStart, dataLength) = FindFftoData(pngBytes);
        var data = new byte[dataLength];
        Buffer.BlockCopy(pngBytes, dataStart, data, 0, dataLength);
        return data;
    }

    public static byte[] Repack(byte[] originalPng, byte[] newPayload)
    {
        ValidateSignature(originalPng);
        var (chunkStart, chunkEnd) = FindFftoChunk(originalPng);

        var newChunkLen = 4 + 4 + newPayload.Length + 4;
        var suffixLen = originalPng.Length - chunkEnd;
        var output = new byte[chunkStart + newChunkLen + suffixLen];

        Buffer.BlockCopy(originalPng, 0, output, 0, chunkStart);
        WriteU32BE(output.AsSpan(chunkStart), (uint)newPayload.Length);
        FftoType.CopyTo(output.AsSpan(chunkStart + 4));
        Buffer.BlockCopy(newPayload, 0, output, chunkStart + 8, newPayload.Length);

        var crcInput = new byte[4 + newPayload.Length];
        FftoType.CopyTo(crcInput);
        Buffer.BlockCopy(newPayload, 0, crcInput, 4, newPayload.Length);
        var crc = Crc32.Compute(crcInput);
        WriteU32BE(output.AsSpan(chunkStart + 8 + newPayload.Length), crc);

        Buffer.BlockCopy(
            originalPng, chunkEnd,
            output, chunkStart + newChunkLen,
            suffixLen);

        return output;
    }

    private static void ValidateSignature(byte[] pngBytes)
    {
        if (pngBytes.Length < 8 || !pngBytes.AsSpan(0, 8).SequenceEqual(Signature))
        {
            throw new InvalidDataException("Not a PNG file (signature mismatch).");
        }
    }

    private static (int dataStart, int dataLength) FindFftoData(byte[] png)
    {
        int pos = 8;
        while (pos + 8 <= png.Length)
        {
            uint length = ReadU32BE(png.AsSpan(pos));
            int chunkEnd = pos + 8 + (int)length + 4;
            if (chunkEnd > png.Length)
            {
                throw new InvalidDataException("Truncated PNG chunk.");
            }
            if (png.AsSpan(pos + 4, 4).SequenceEqual(FftoType))
            {
                return (pos + 8, (int)length);
            }
            pos = chunkEnd;
        }
        throw new InvalidDataException("PNG does not contain an ffTo chunk.");
    }

    private static (int chunkStart, int chunkEnd) FindFftoChunk(byte[] png)
    {
        int pos = 8;
        while (pos + 8 <= png.Length)
        {
            uint length = ReadU32BE(png.AsSpan(pos));
            int chunkEnd = pos + 8 + (int)length + 4;
            if (chunkEnd > png.Length)
            {
                throw new InvalidDataException("Truncated PNG chunk.");
            }
            if (png.AsSpan(pos + 4, 4).SequenceEqual(FftoType))
            {
                return (pos, chunkEnd);
            }
            pos = chunkEnd;
        }
        throw new InvalidDataException("PNG does not contain an ffTo chunk.");
    }

    private static uint ReadU32BE(ReadOnlySpan<byte> b)
        => ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];

    private static void WriteU32BE(Span<byte> b, uint v)
    {
        b[0] = (byte)(v >> 24);
        b[1] = (byte)(v >> 16);
        b[2] = (byte)(v >> 8);
        b[3] = (byte)v;
    }
}

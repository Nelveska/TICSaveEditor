// Adapted from Nenkai/FF16Tools (MIT) — see THIRD_PARTY_LICENSES.md.
//
// UMIF is Square Enix's container format used in FF16 / FFT:TIC saves. After
// PNG-envelope extraction, the `ffTo` chunk holds a UMIF blob that wraps
// the actual saveWork payload. v0.1 supports only single-entry UMIFs
// (numFiles = 1) — what TIC's manual saves emit.

using System.Buffers.Binary;
using System.Text;
using ICSharpCode.SharpZipLib.Zip.Compression;
using TICSaveEditor.Core.Util;

namespace TICSaveEditor.Core.Save;

internal static class UmifContainer
{
    public const string DefaultFilename = "fftsave.bin";
    private const uint UmifMagic = 0x46494D55; // "UMIF" LE
    private const ulong XorKey = 0x0F3F80FE5F1FC4F3UL;
    private const int MainHeaderSize = 0x10;
    private const int FileEntrySize = 0x20;

    public static byte[] Unpack(byte[] umifBytes)
    {
        ArgumentNullException.ThrowIfNull(umifBytes);
        if (umifBytes.Length < MainHeaderSize)
            throw new InvalidDataException(
                $"UMIF blob too short: {umifBytes.Length} bytes (need at least {MainHeaderSize}).");

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(umifBytes.AsSpan(0x08, 4));
        if (magic != UmifMagic)
            throw new InvalidDataException(
                $"Not a UMIF blob: magic = 0x{magic:X8}, expected 0x{UmifMagic:X8}.");

        var numFiles = BinaryPrimitives.ReadUInt32LittleEndian(umifBytes.AsSpan(0x0C, 4));
        if (numFiles != 1)
            throw new NotSupportedException(
                $"Multi-file UMIF (numFiles={numFiles}) not supported in v0.1.");

        if (umifBytes.Length < MainHeaderSize + FileEntrySize)
            throw new InvalidDataException("UMIF blob truncated before file entry.");

        var entry = umifBytes.AsSpan(MainHeaderSize, FileEntrySize);
        var compressedLen = (int)BinaryPrimitives.ReadUInt32LittleEndian(entry.Slice(0x04, 4));
        var decompressedLen = (int)BinaryPrimitives.ReadInt64LittleEndian(entry.Slice(0x10, 8));
        var compressedDataPtr = (int)BinaryPrimitives.ReadInt64LittleEndian(entry.Slice(0x18, 8));

        if (compressedDataPtr + compressedLen > umifBytes.Length)
            throw new InvalidDataException("UMIF compressed-data pointer/length out of range.");

        var encrypted = umifBytes.AsSpan(compressedDataPtr, compressedLen).ToArray();
        Crypt(encrypted);

        // Prepend zlib header bytes 0x78 0xF9 (CMF=0x78 windowBits=7+8=15; FLG with FDICT=1).
        // Without these, the inflater wouldn't know to expect a preset dictionary.
        var zlibInput = new byte[2 + encrypted.Length];
        zlibInput[0] = 0x78;
        zlibInput[1] = 0xF9;
        encrypted.CopyTo(zlibInput.AsSpan(2));

        var inflated = new byte[decompressedLen];
        var inflater = new Inflater(noHeader: false);
        inflater.SetInput(zlibInput);
        var written = inflater.Inflate(inflated);
        if (inflater.IsNeedingDictionary)
        {
            inflater.SetDictionary(UmifCompressDict.Bytes);
            written += inflater.Inflate(inflated, written, inflated.Length - written);
        }
        if (written != decompressedLen)
            throw new InvalidDataException(
                $"UMIF inflate length mismatch: got {written}, expected {decompressedLen}.");

        return inflated;
    }

    public static byte[] Pack(byte[] payload, string filename = DefaultFilename)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(filename);

        var working = (byte[])payload.Clone();

        // CRC32 fix-up: binary payloads (i.e., not "<?xml ...") get a CRC of bytes from
        // offset 0x10 onward written as u32 LE at offset 0x04 of the payload.
        if (!StartsWithXmlMarker(working))
        {
            FixCrcInPlace(working);
        }

        var nameBytes = Encoding.UTF8.GetBytes(filename);
        var nameWithNull = new byte[nameBytes.Length + 1];
        nameBytes.CopyTo(nameWithNull, 0);
        nameWithNull[^1] = 0x00;
        var encryptedName = (byte[])nameWithNull.Clone();
        Crypt(encryptedName);

        // Compress with preset dictionary, then strip the leading 2 zlib header
        // bytes (CMF + FLG). What remains: DICTID(4) + deflate-body + ADLER32(4).
        var compressed = CompressWithDictionary(working);
        if (compressed.Length < 2)
            throw new InvalidOperationException("Deflate output unexpectedly short.");
        var compressedAfterHeader = compressed.AsSpan(2).ToArray();
        Crypt(compressedAfterHeader);

        // Layout: [0..0x10) main header
        //         [0x10..0x30) file entry (single)
        //         [0x30..0x30+nameLen) encrypted filename
        //         (4-byte align padding)
        //         [...) encrypted compressed data
        //         [...] trailing null byte (FF16Tools writes one after each entry's data)

        var nameOffset = MainHeaderSize + FileEntrySize;
        var nameAlignedEnd = AlignUp(nameOffset + encryptedName.Length, 4);
        var dataOffset = nameAlignedEnd;
        var totalLen = dataOffset + compressedAfterHeader.Length + 1;

        var output = new byte[totalLen];

        // Main header.
        var hdr = output.AsSpan(0, MainHeaderSize);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr.Slice(0x00, 4), MainHeaderSize + FileEntrySize);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr.Slice(0x04, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr.Slice(0x08, 4), UmifMagic);
        BinaryPrimitives.WriteUInt32LittleEndian(hdr.Slice(0x0C, 4), 1);

        // File entry at 0x10.
        var entry = output.AsSpan(MainHeaderSize, FileEntrySize);
        BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(0x00, 4), (uint)nameWithNull.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(entry.Slice(0x04, 4), (uint)compressedAfterHeader.Length);
        BinaryPrimitives.WriteInt64LittleEndian(entry.Slice(0x08, 8), nameOffset);
        BinaryPrimitives.WriteInt64LittleEndian(entry.Slice(0x10, 8), payload.Length);
        BinaryPrimitives.WriteInt64LittleEndian(entry.Slice(0x18, 8), dataOffset);

        // Filename.
        encryptedName.CopyTo(output.AsSpan(nameOffset));
        // (4-byte align padding is already zero from the new byte[] allocation.)

        // Compressed data + trailing null.
        compressedAfterHeader.CopyTo(output.AsSpan(dataOffset));
        // Trailing null byte at totalLen-1 is already zero.

        return output;
    }

    private static byte[] CompressWithDictionary(byte[] input)
    {
        var deflater = new Deflater(Deflater.BEST_COMPRESSION, noZlibHeaderOrFooter: false);
        deflater.SetDictionary(UmifCompressDict.Bytes);
        deflater.SetInput(input);
        deflater.Finish();

        // Output buffer sized generously: zlib worst-case is input + 6 + 5 per 16K block.
        var outBuf = new byte[input.Length + 64 + (input.Length / 16384 + 1) * 5];
        var written = 0;
        while (!deflater.IsFinished)
        {
            var n = deflater.Deflate(outBuf, written, outBuf.Length - written);
            if (n == 0) break;
            written += n;
        }
        var result = new byte[written];
        Array.Copy(outBuf, result, written);
        return result;
    }

    private static void FixCrcInPlace(byte[] payload)
    {
        if (payload.Length < 0x10) return;
        var crc = Crc32.Compute(payload.AsSpan(0x10));
        BinaryPrimitives.WriteUInt32LittleEndian(payload.AsSpan(0x04, 4), crc);
    }

    private static bool StartsWithXmlMarker(byte[] payload)
    {
        const string Marker = "<?xml version=\"1.0\"?>";
        if (payload.Length < Marker.Length) return false;
        var bytes = Encoding.UTF8.GetBytes(Marker);
        return payload.AsSpan(0, bytes.Length).SequenceEqual(bytes);
    }

    private static int AlignUp(int value, int alignment)
        => (value + alignment - 1) & ~(alignment - 1);

    /// <summary>
    /// XOR cipher used by UMIF to obscure filenames and compressed data.
    /// Operates in place. Same operation for encrypt and decrypt (XOR is its own inverse).
    /// </summary>
    public static void Crypt(Span<byte> data)
    {
        var pos = 0;
        // Eight-byte chunks.
        while (data.Length - pos >= 8)
        {
            var v = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(pos, 8));
            BinaryPrimitives.WriteUInt64LittleEndian(data.Slice(pos, 8), v ^ XorKey);
            pos += 8;
        }
        // Four-byte tail.
        if (data.Length - pos >= 4)
        {
            var v = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(pos, 4));
            BinaryPrimitives.WriteUInt32LittleEndian(data.Slice(pos, 4), v ^ (uint)(XorKey & 0xFFFFFFFFu));
            pos += 4;
        }
        // Two-byte tail.
        if (data.Length - pos >= 2)
        {
            var v = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(pos, 2));
            BinaryPrimitives.WriteUInt16LittleEndian(data.Slice(pos, 2), (ushort)(v ^ (XorKey & 0xFFFFu)));
            pos += 2;
        }
        // One-byte tail.
        if (data.Length - pos >= 1)
        {
            data[pos] = (byte)(data[pos] ^ (byte)(XorKey & 0xFFu));
        }
    }
}

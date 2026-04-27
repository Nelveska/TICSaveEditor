using System.Runtime.InteropServices;

namespace TICSaveEditor.Core.Save;

public class FftiHeader
{
    public const uint MagicValue = 0x49544646;
    public const int KnownPrefixSize = 0x7A;

    private readonly byte[] _bytes;

    public FftiHeader(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < KnownPrefixSize)
        {
            throw new ArgumentException(
                $"FFTI header requires at least {KnownPrefixSize} bytes (got {bytes.Length}).",
                nameof(bytes));
        }
        _bytes = bytes.ToArray();
    }

    public byte[] RawBytes => _bytes;

    public uint Magic => MemoryMarshal.Read<uint>(_bytes.AsSpan(0x00, 4));
    public uint ThisHeaderSize => MemoryMarshal.Read<uint>(_bytes.AsSpan(0x04, 4));
    public uint Size => MemoryMarshal.Read<uint>(_bytes.AsSpan(0x08, 4));
    public uint SaveType => MemoryMarshal.Read<uint>(_bytes.AsSpan(0x0C, 4));
    public uint UnkMemorySize => MemoryMarshal.Read<uint>(_bytes.AsSpan(0x10, 4));

    public DateTime SaveTimestamp
    {
        get
        {
            var seconds = MemoryMarshal.Read<long>(_bytes.AsSpan(0x38, 8));
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }
    }

    public byte[] UnmappedTail
    {
        get
        {
            if (_bytes.Length <= 0x154)
            {
                return Array.Empty<byte>();
            }
            var tail = new byte[_bytes.Length - 0x154];
            Buffer.BlockCopy(_bytes, 0x154, tail, 0, tail.Length);
            return tail;
        }
    }
}

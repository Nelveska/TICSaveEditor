using System.Buffers.Binary;
using System.Text;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class CardSection : SaveWorkSection
{
    private const int MagicOffset = 0x00;
    private const int TitleOffset = 0x04;
    private const int TitleLength = 0x40;
    private const int TimestampOffset = 0x44;
    private const int IconOffset = 0x80;
    private const int IconLength = 0x80;

    internal CardSection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.CardSize;

    public ushort Magic =>
        BinaryPrimitives.ReadUInt16LittleEndian(Bytes.AsSpan(MagicOffset, 2));

    public string Title
    {
        get
        {
            var span = Bytes.AsSpan(TitleOffset, TitleLength);
            var nullIdx = span.IndexOf((byte)0);
            var len = nullIdx < 0 ? TitleLength : nullIdx;
            return Encoding.ASCII.GetString(span.Slice(0, len));
        }
        set
        {
            var dest = Bytes.AsSpan(TitleOffset, TitleLength);
            var v = value ?? string.Empty;
            var clampedLen = Math.Min(v.Length, TitleLength - 1);
            var bytesWritten = Encoding.ASCII.GetBytes(v.AsSpan(0, clampedLen), dest);
            if (bytesWritten < TitleLength)
            {
                dest[bytesWritten] = 0;
            }
            OnPropertyChanged();
        }
    }

    public DateTime SaveTimestamp
    {
        get
        {
            var seconds = BinaryPrimitives.ReadInt32LittleEndian(
                Bytes.AsSpan(TimestampOffset, 4));
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }
    }

    public byte[] IconRaw
    {
        get
        {
            var icon = new byte[IconLength];
            Bytes.AsSpan(IconOffset, IconLength).CopyTo(icon);
            return icon;
        }
    }
}

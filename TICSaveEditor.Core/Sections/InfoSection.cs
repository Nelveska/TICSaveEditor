using System.Buffers.Binary;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class InfoSection : SaveWorkSection
{
    public const int HeroNameLength = 17;
    public const int InternalChecksumLength = 16;
    public const int InfoTrailingLength = 64;

    private const int HeroNameOffset = 0x01;
    private const int NextEventIdOffset = 0x1C;
    private const int MainProgressOffset = 0x20;
    private const int InternalChecksumOffset = 0x64;
    private const int PlaytimeOffset = 0x74;
    private const int InfoTrailingOffset = 0x78;

    internal InfoSection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.InfoSize;

    public byte[] HeroNameRaw
    {
        get
        {
            var copy = new byte[HeroNameLength];
            Bytes.AsSpan(HeroNameOffset, HeroNameLength).CopyTo(copy);
            return copy;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length != HeroNameLength)
                throw new ArgumentException(
                    $"HeroNameRaw must be exactly {HeroNameLength} bytes (got {value.Length}).",
                    nameof(value));
            if (Bytes.AsSpan(HeroNameOffset, HeroNameLength).SequenceEqual(value)) return;
            value.CopyTo(Bytes.AsSpan(HeroNameOffset, HeroNameLength));
            OnPropertyChanged();
        }
    }

    public int NextEventId =>
        BinaryPrimitives.ReadInt32LittleEndian(Bytes.AsSpan(NextEventIdOffset, 4));

    public int MainProgress =>
        BinaryPrimitives.ReadInt32LittleEndian(Bytes.AsSpan(MainProgressOffset, 4));

    // Spec at tic-save-editor-api-surface.md:589 specs `uint InternalChecksum`,
    // but Nenkai's 010 template declares `chk_sum byte[0x10]` — 16 bytes.
    // We follow the template per byte-faithful invariant; recompute-on-write
    // is deferred until the digest algorithm is identified.
    public byte[] InternalChecksumRaw
    {
        get
        {
            var copy = new byte[InternalChecksumLength];
            Bytes.AsSpan(InternalChecksumOffset, InternalChecksumLength).CopyTo(copy);
            return copy;
        }
    }

    public TimeSpan Playtime
    {
        get => TimeSpan.FromSeconds(
            BinaryPrimitives.ReadInt32LittleEndian(Bytes.AsSpan(PlaytimeOffset, 4)));
        set
        {
            if (value < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(value),
                    "Playtime cannot be negative.");
            var secs = (int)Math.Floor(value.TotalSeconds);
            if (BinaryPrimitives.ReadInt32LittleEndian(Bytes.AsSpan(PlaytimeOffset, 4)) == secs)
                return;
            BinaryPrimitives.WriteInt32LittleEndian(Bytes.AsSpan(PlaytimeOffset, 4), secs);
            OnPropertyChanged();
        }
    }

    public byte[] InfoTrailingRaw
    {
        get
        {
            var copy = new byte[InfoTrailingLength];
            Bytes.AsSpan(InfoTrailingOffset, InfoTrailingLength).CopyTo(copy);
            return copy;
        }
    }
}

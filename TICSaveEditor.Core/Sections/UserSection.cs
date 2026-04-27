using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class UserSection : SaveWorkSection
{
    internal UserSection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.UserSize;

    public byte[] GameProgressRaw => Bytes.AsSpan(0x00, 48).ToArray();
    public byte[] GameFlagRaw     => Bytes.AsSpan(0x30, 32).ToArray();
    public byte[] BonusItemsRaw   => Bytes.AsSpan(0x50, 20).ToArray();
}

using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class WorldSection : SaveWorkSection
{
    internal WorldSection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.WorldSize;

    public byte[] TreasureFindDayRaw => Bytes.AsSpan(0x000, 53).ToArray();
    public byte[] UnregFindDayRaw    => Bytes.AsSpan(0x035, 18).ToArray();
    public byte[] MoukeFinishDayRaw  => Bytes.AsSpan(0x047, 108).ToArray();
    public byte[] MoukeDelayRaw      => Bytes.AsSpan(0x0B3, 96).ToArray();
    public byte[] SnplInfRaw         => Bytes.AsSpan(0x114, 200).ToArray();
    public byte[] SnplPageFlagRaw    => Bytes.AsSpan(0x1DC, 160).ToArray();
    public byte[] SnplStaticFlagRaw  => Bytes.AsSpan(0x27C, 8).ToArray();
    public byte[] PersonYearRaw      => Bytes.AsSpan(0x284, 64).ToArray();
    public byte[] MoukeEventRaw      => Bytes.AsSpan(0x2C5, 64).ToArray();
    public byte[] WorldTrailingRaw   => Bytes.AsSpan(0x308, 88).ToArray();
}

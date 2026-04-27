using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class FftoAchievementSection : SaveWorkSection
{
    internal FftoAchievementSection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.FftoAchievementSize;

    public byte[] UnlockedRaw       => Bytes.AsSpan(0x00, 50).ToArray();
    public byte[] ProgressRaw       => Bytes.AsSpan(0x32, 50).ToArray();
    public byte[] PoachItemTypeRaw  => Bytes.AsSpan(0x64, 26).ToArray();
    public byte[] SummonTypeRaw     => Bytes.AsSpan(0x7E, 16).ToArray();
    public byte[] GeomancyTypeRaw   => Bytes.AsSpan(0x8E, 12).ToArray();
    public byte[] SongTypeRaw       => Bytes.AsSpan(0x9A, 7).ToArray();
    public byte[] IaidoTypeRaw      => Bytes.AsSpan(0xA1, 1).ToArray();
    public byte[] DanceTurnsRaw     => Bytes.AsSpan(0xA2, 10).ToArray();
}

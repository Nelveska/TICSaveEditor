using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class FftoBraveStorySection : SaveWorkSection
{
    internal FftoBraveStorySection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.FftoBraveStorySize;

    public byte[] ZodiacStoneRaw         => Bytes.AsSpan(0x000, 52).ToArray();
    public byte[] BookRaw                => Bytes.AsSpan(0x034, 6).ToArray();
    public byte[] JournalRaw             => Bytes.AsSpan(0x03A, 520).ToArray();
    public byte[] GlossaryRaw            => Bytes.AsSpan(0x242, 440).ToArray();
    public byte[] WorldSituationRaw      => Bytes.AsSpan(0x3FA, 3).ToArray();
    public byte[] BraveStoryTrailingRaw  => Bytes.AsSpan(0x3FD, 152).ToArray();
}

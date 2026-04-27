using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class FftoWorldSection : SaveWorkSection
{
    internal FftoWorldSection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.FftoWorldSize;

    public byte[] RawBytes => Bytes.AsSpan(0, Size).ToArray();
}

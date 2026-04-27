using TICSaveEditor.Core.Save;

namespace TICSaveEditor.Core.Sections;

public class FftoConfigSection : SaveWorkSection
{
    private const int DifficultyLevelOffset = 0x00;

    internal FftoConfigSection(ReadOnlySpan<byte> bytes) : base(bytes) { }

    internal override int Size => SaveWorkLayout.FftoConfigSize;

    public byte DifficultyLevel
    {
        get => Bytes[DifficultyLevelOffset];
        set
        {
            if (Bytes[DifficultyLevelOffset] == value) return;
            Bytes[DifficultyLevelOffset] = value;
            OnPropertyChanged();
        }
    }
}

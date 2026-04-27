namespace TICSaveEditor.Core.Save;

internal static class SaveWorkLayout
{
    public const int CardOffset = 0x0000;
    public const int CardSize = 0x0100;

    public const int InfoOffset = 0x0100;
    public const int InfoSize = 0x00B8;

    public const int WorldOffset = 0x01B8;
    public const int WorldSize = 0x0360;

    public const int BattleOffset = 0x0518;
    public const int BattleSize = 0x8F48;

    public const int UserOffset = 0x9460;
    public const int UserSize = 0x0064;

    public const int FftoWorldOffset = 0x94C4;
    public const int FftoWorldSize = 0x0208;

    public const int FftoBattleOffset = 0x96CC;
    public const int FftoBattleSize = 0x00C2;

    public const int FftoAchievementOffset = 0x978E;
    public const int FftoAchievementSize = 0x00AC;

    public const int FftoConfigOffset = 0x983A;
    public const int FftoConfigSize = 0x0001;

    public const int FftoBraveStoryOffset = 0x983B;
    public const int FftoBraveStorySize = 0x0495;

    public const int TrailingUnkOffset = 0x9CD0;
    public const int TrailingUnkSize = 0x000C;

    public const int TotalSize = 0x9CDC;

    public static readonly (string Name, int Offset, int Size)[] Entries = new[]
    {
        ("Card",            CardOffset,            CardSize),
        ("Info",            InfoOffset,            InfoSize),
        ("World",           WorldOffset,           WorldSize),
        ("Battle",          BattleOffset,          BattleSize),
        ("User",            UserOffset,            UserSize),
        ("FftoWorld",       FftoWorldOffset,       FftoWorldSize),
        ("FftoBattle",      FftoBattleOffset,      FftoBattleSize),
        ("FftoAchievement", FftoAchievementOffset, FftoAchievementSize),
        ("FftoConfig",      FftoConfigOffset,      FftoConfigSize),
        ("FftoBraveStory",  FftoBraveStoryOffset,  FftoBraveStorySize),
        ("TrailingUnk",     TrailingUnkOffset,     TrailingUnkSize),
    };
}

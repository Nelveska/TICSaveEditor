namespace TICSaveEditor.Core.GameData;

public record JobInfo(
    int Id,
    string Name,
    string Description,
    int JobTypeId,
    int JobCommandId,
    byte HpGrowth,
    byte HpMultiplier,
    byte MpGrowth,
    byte MpMultiplier,
    byte SpeedGrowth,
    byte SpeedMultiplier,
    byte PaGrowth,
    byte PaMultiplier,
    byte MaGrowth,
    byte MaMultiplier,
    byte Move,
    byte Jump,
    byte CharacterEvasion);

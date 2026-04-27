namespace TICSaveEditor.Core.GameData.Xml;

internal record JobDataXmlEntry(
    int Id,
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

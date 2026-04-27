namespace TICSaveEditor.Core.GameData;

public record ItemInfo(
    int Id,
    string Name,
    string Description,
    string NameSingular,
    string NamePlural,
    string ItemCategory,
    int Price,
    byte RequiredLevel);

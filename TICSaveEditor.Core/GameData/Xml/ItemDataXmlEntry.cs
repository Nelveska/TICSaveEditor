namespace TICSaveEditor.Core.GameData.Xml;

internal record ItemDataXmlEntry(
    int Id,
    string ItemCategory,
    int Price,
    byte RequiredLevel);

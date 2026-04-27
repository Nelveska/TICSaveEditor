using System.Xml.Linq;

namespace TICSaveEditor.Core.GameData.Xml;

internal sealed class ItemDataXmlReader
{
    private const string TableLabel = "ItemDataXmlReader";

    // Modloader v1.7.0 ItemData fields not consumed by v0.1 ItemInfo. Skip silently.
    private static readonly HashSet<string> KnownUnusedElements = new(StringComparer.Ordinal)
    {
        "Palette", "SpriteID", "TypeFlags", "AdditionalDataId",
        "Unused_0x06", "EquipBonusId", "ShopAvailability", "Unused_0x0B",
    };

    private readonly IGameDataLogger _logger;

    public ItemDataXmlReader(IGameDataLogger? logger = null)
    {
        _logger = logger ?? NullGameDataLogger.Instance;
    }

    public IReadOnlyList<ItemDataXmlEntry> Read(Stream xmlStream)
    {
        if (xmlStream is null) throw new ArgumentNullException(nameof(xmlStream));

        var doc = XDocument.Load(xmlStream);
        var root = doc.Root
            ?? throw new InvalidDataException("ItemData.xml has no root element.");

        var entries = root.Element("Entries")
            ?? throw new InvalidDataException("ItemData.xml is missing the <Entries> element.");

        var result = new List<ItemDataXmlEntry>();
        foreach (var itemElement in entries.Elements("Item"))
        {
            result.Add(ParseItem(itemElement));
        }
        return result;
    }

    private ItemDataXmlEntry ParseItem(XElement itemElement)
    {
        int? id = null;
        string? itemCategory = null;
        int? price = null;
        byte? requiredLevel = null;

        foreach (var child in itemElement.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "Id":            id = XmlParseHelpers.ParseInt(child, name, id, TableLabel); break;
                case "ItemCategory":  itemCategory = XmlParseHelpers.ParseString(child); break;
                case "Price":         price = XmlParseHelpers.ParseInt(child, name, id, TableLabel); break;
                case "RequiredLevel": requiredLevel = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                default:
                    if (!KnownUnusedElements.Contains(name))
                    {
                        _logger.LogWarning(
                            $"{TableLabel}: skipping unknown element <{name}> in Item entry " +
                            $"{(id.HasValue ? $"Id={id}" : "(Id not yet seen)")}.");
                    }
                    break;
            }
        }

        if (id is null) throw XmlParseHelpers.MissingField(TableLabel, "Id", null);
        if (itemCategory is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(ItemDataXmlEntry.ItemCategory), id);
        if (price is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(ItemDataXmlEntry.Price), id);
        if (requiredLevel is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(ItemDataXmlEntry.RequiredLevel), id);

        return new ItemDataXmlEntry(
            Id: id.Value,
            ItemCategory: itemCategory,
            Price: price.Value,
            RequiredLevel: requiredLevel.Value);
    }
}

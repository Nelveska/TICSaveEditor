using System.Xml.Linq;

namespace TICSaveEditor.Core.GameData.Xml;

internal sealed class AbilityDataXmlReader
{
    private const string TableLabel = "AbilityDataXmlReader";

    // Modloader v1.7.0 AbilityData fields not consumed by v0.1 AbilityInfo. Skip silently.
    private static readonly HashSet<string> KnownUnusedElements = new(StringComparer.Ordinal)
    {
        "JPCost",          // modloader docs: "Only JPCost from the Ability nex table is used. This one is unused!"
        "Flags",           // flag-enum string, deferred
        "AIBehaviorFlags", // flag-enum string, AI-only
    };

    private readonly IGameDataLogger _logger;

    public AbilityDataXmlReader(IGameDataLogger? logger = null)
    {
        _logger = logger ?? NullGameDataLogger.Instance;
    }

    public IReadOnlyList<AbilityDataXmlEntry> Read(Stream xmlStream)
    {
        if (xmlStream is null) throw new ArgumentNullException(nameof(xmlStream));

        var doc = XDocument.Load(xmlStream);
        var root = doc.Root
            ?? throw new InvalidDataException("AbilityData.xml has no root element.");

        var entries = root.Element("Entries")
            ?? throw new InvalidDataException("AbilityData.xml is missing the <Entries> element.");

        var result = new List<AbilityDataXmlEntry>();
        foreach (var abilityElement in entries.Elements("Ability"))
        {
            result.Add(ParseAbility(abilityElement));
        }
        return result;
    }

    private AbilityDataXmlEntry ParseAbility(XElement abilityElement)
    {
        int? id = null;
        byte? chanceToLearn = null;
        string? abilityType = null;

        foreach (var child in abilityElement.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "Id":             id = XmlParseHelpers.ParseInt(child, name, id, TableLabel); break;
                case "ChanceToLearn":  chanceToLearn = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "AbilityType":    abilityType = XmlParseHelpers.ParseString(child); break;
                default:
                    if (!KnownUnusedElements.Contains(name))
                    {
                        _logger.LogWarning(
                            $"{TableLabel}: skipping unknown element <{name}> in Ability entry " +
                            $"{(id.HasValue ? $"Id={id}" : "(Id not yet seen)")}.");
                    }
                    break;
            }
        }

        if (id is null) throw XmlParseHelpers.MissingField(TableLabel, "Id", null);
        if (chanceToLearn is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(AbilityDataXmlEntry.ChanceToLearn), id);
        if (abilityType is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(AbilityDataXmlEntry.AbilityType), id);

        return new AbilityDataXmlEntry(
            Id: id.Value,
            ChanceToLearn: chanceToLearn.Value,
            AbilityType: abilityType);
    }
}

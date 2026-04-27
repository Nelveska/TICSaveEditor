using System.Xml.Linq;

namespace TICSaveEditor.Core.GameData.Xml;

internal sealed class JobDataXmlReader
{
    private const string TableLabel = "JobDataXmlReader";

    // Modloader v1.7.0 fields not consumed by v0.1 JobInfo. Skip silently;
    // truly-unknown fields warn (forward-compat per decisions_m7_reader_skip_semantics.md).
    private static readonly HashSet<string> KnownUnusedElements = new(StringComparer.Ordinal)
    {
        "JobCommandId",       // sourced from Nex (authoritative) per spec §8.2
        "InnateAbilityId1", "InnateAbilityId2", "InnateAbilityId3", "InnateAbilityId4",
        "EquippableItems",
        "InnateStatus", "ImmuneStatus", "StartingStatus",
        "AbsorbElements", "NullifyElements", "HalveElements", "WeakElements",
        "MonsterPortrait", "MonsterPalette", "MonsterGraphic",
    };

    private readonly IGameDataLogger _logger;

    public JobDataXmlReader(IGameDataLogger? logger = null)
    {
        _logger = logger ?? NullGameDataLogger.Instance;
    }

    public IReadOnlyList<JobDataXmlEntry> Read(Stream xmlStream)
    {
        if (xmlStream is null) throw new ArgumentNullException(nameof(xmlStream));

        var doc = XDocument.Load(xmlStream);
        var root = doc.Root
            ?? throw new InvalidDataException("JobData.xml has no root element.");

        var entries = root.Element("Entries")
            ?? throw new InvalidDataException("JobData.xml is missing the <Entries> element.");

        var result = new List<JobDataXmlEntry>();
        foreach (var jobElement in entries.Elements("Job"))
        {
            result.Add(ParseJob(jobElement));
        }
        return result;
    }

    private JobDataXmlEntry ParseJob(XElement jobElement)
    {
        int? id = null;
        byte? hpGrowth = null, hpMultiplier = null;
        byte? mpGrowth = null, mpMultiplier = null;
        byte? speedGrowth = null, speedMultiplier = null;
        byte? paGrowth = null, paMultiplier = null;
        byte? maGrowth = null, maMultiplier = null;
        byte? move = null, jump = null, characterEvasion = null;

        foreach (var child in jobElement.Elements())
        {
            var name = child.Name.LocalName;
            switch (name)
            {
                case "Id":               id = XmlParseHelpers.ParseInt(child, name, id, TableLabel); break;
                case "HPGrowth":         hpGrowth = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "HPMultiplier":     hpMultiplier = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "MPGrowth":         mpGrowth = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "MPMultiplier":     mpMultiplier = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "SpeedGrowth":      speedGrowth = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "SpeedMultiplier":  speedMultiplier = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "PAGrowth":         paGrowth = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "PAMultiplier":     paMultiplier = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "MAGrowth":         maGrowth = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "MAMultiplier":     maMultiplier = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "Move":             move = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "Jump":             jump = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                case "CharacterEvasion": characterEvasion = XmlParseHelpers.ParseByte(child, name, id, TableLabel); break;
                default:
                    if (!KnownUnusedElements.Contains(name))
                    {
                        _logger.LogWarning(
                            $"{TableLabel}: skipping unknown element <{name}> in Job entry " +
                            $"{(id.HasValue ? $"Id={id}" : "(Id not yet seen)")}.");
                    }
                    break;
            }
        }

        if (id is null) throw XmlParseHelpers.MissingField(TableLabel, "Id", null);
        if (hpGrowth is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.HpGrowth), id);
        if (hpMultiplier is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.HpMultiplier), id);
        if (mpGrowth is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.MpGrowth), id);
        if (mpMultiplier is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.MpMultiplier), id);
        if (speedGrowth is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.SpeedGrowth), id);
        if (speedMultiplier is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.SpeedMultiplier), id);
        if (paGrowth is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.PaGrowth), id);
        if (paMultiplier is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.PaMultiplier), id);
        if (maGrowth is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.MaGrowth), id);
        if (maMultiplier is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.MaMultiplier), id);
        if (move is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.Move), id);
        if (jump is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.Jump), id);
        if (characterEvasion is null) throw XmlParseHelpers.MissingField(TableLabel, nameof(JobDataXmlEntry.CharacterEvasion), id);

        return new JobDataXmlEntry(
            Id: id.Value,
            HpGrowth: hpGrowth.Value,
            HpMultiplier: hpMultiplier.Value,
            MpGrowth: mpGrowth.Value,
            MpMultiplier: mpMultiplier.Value,
            SpeedGrowth: speedGrowth.Value,
            SpeedMultiplier: speedMultiplier.Value,
            PaGrowth: paGrowth.Value,
            PaMultiplier: paMultiplier.Value,
            MaGrowth: maGrowth.Value,
            MaMultiplier: maMultiplier.Value,
            Move: move.Value,
            Jump: jump.Value,
            CharacterEvasion: characterEvasion.Value);
    }
}

using System.Reflection;
using TICSaveEditor.Core.GameData.Nex;
using TICSaveEditor.Core.GameData.Xml;

namespace TICSaveEditor.Core.GameData;

public class GameDataLoader
{
    private const string ResourceNamespace = "TICSaveEditor.Core.Resources";
    private const string ModloaderResourceFolder = "Modloader";
    private const string NexResourceFolder = "Nex";

    private readonly Assembly _resourceAssembly;
    private readonly IGameDataLogger _logger;

    public GameDataLoader() : this(null, null) { }

    internal GameDataLoader(IGameDataLogger? logger = null, Assembly? resourceAssembly = null)
    {
        _logger = logger ?? NullGameDataLogger.Instance;
        _resourceAssembly = resourceAssembly ?? typeof(GameDataLoader).Assembly;
    }

    public GameDataContext LoadBundled(string language = "en")
    {
        if (string.IsNullOrEmpty(language))
            throw new ArgumentException("Language must not be null or empty.", nameof(language));

        // XML is language-invariant. Nex is per-locale; non-en falls back to empty per
        // decisions_m7_partial_language_state.md. Each table joins XML+Nex (or Nex-only).
        var jobs = JoinJob(
            ReadBundledXml<JobDataXmlEntry>("JobData.xml", new JobDataXmlReader(_logger)),
            ReadBundledNex<JobNexEntry>(language, "Job.json", new JobNexCatalogReader()));

        var items = JoinItem(
            ReadBundledXml<ItemDataXmlEntry>("ItemData.xml", new ItemDataXmlReader(_logger)),
            ReadBundledNex<ItemNexEntry>(language, "Item.json", new ItemNexCatalogReader()));

        var abilities = JoinAbility(
            ReadBundledXml<AbilityDataXmlEntry>("AbilityData.xml", new AbilityDataXmlReader(_logger)),
            ReadBundledNex<AbilityNexEntry>(language, "Ability.json", new AbilityNexCatalogReader()));

        var jobCommands = JoinJobCommand(
            ReadBundledNex<JobCommandNexEntry>(language, "JobCommand.json", new JobCommandNexCatalogReader()));

        var statusEffects = JoinStatusEffect(
            ReadBundledNex<StatusEffectNexEntry>(language, "UIStatusEffect.json", new StatusEffectNexCatalogReader()));

        var charaNames = JoinCharaName(
            ReadBundledNex<CharaNameNexEntry>(language, "CharaName.json", new CharaNameNexCatalogReader()));

        return BuildContext(language, GameDataSource.Bundled, "<bundled>",
            jobs, items, abilities, jobCommands, statusEffects, charaNames);
    }

    public GameDataContext LoadUserOverride(string tablesDirectory, string language = "en")
    {
        if (string.IsNullOrEmpty(tablesDirectory))
            throw new ArgumentException("Tables directory must not be null or empty.", nameof(tablesDirectory));
        if (string.IsNullOrEmpty(language))
            throw new ArgumentException("Language must not be null or empty.", nameof(language));

        // v0.1 uses 'enhanced' mode only per spec §8.3.
        var modeDir = Path.Combine(tablesDirectory, "enhanced");
        var jobXmlPath = Path.Combine(modeDir, "JobData.xml");
        if (!File.Exists(jobXmlPath))
            throw new FileNotFoundException($"Override JobData.xml not found at {jobXmlPath}.", jobXmlPath);

        // For now, only JobData supports override (matches spec §8.3 v0.1 scope: override XMLs;
        // Nex catalog override is v0.2+). Other table XMLs come from bundled.
        var jobsXml = ReadFileXml<JobDataXmlEntry>(jobXmlPath, new JobDataXmlReader(_logger));
        var jobs = JoinJob(jobsXml, ReadBundledNex<JobNexEntry>(language, "Job.json", new JobNexCatalogReader()));

        var itemXmlPath = Path.Combine(modeDir, "ItemData.xml");
        var itemsXml = File.Exists(itemXmlPath)
            ? ReadFileXml<ItemDataXmlEntry>(itemXmlPath, new ItemDataXmlReader(_logger))
            : ReadBundledXml<ItemDataXmlEntry>("ItemData.xml", new ItemDataXmlReader(_logger));
        var items = JoinItem(itemsXml, ReadBundledNex<ItemNexEntry>(language, "Item.json", new ItemNexCatalogReader()));

        var abilityXmlPath = Path.Combine(modeDir, "AbilityData.xml");
        var abilitiesXml = File.Exists(abilityXmlPath)
            ? ReadFileXml<AbilityDataXmlEntry>(abilityXmlPath, new AbilityDataXmlReader(_logger))
            : ReadBundledXml<AbilityDataXmlEntry>("AbilityData.xml", new AbilityDataXmlReader(_logger));
        var abilities = JoinAbility(abilitiesXml, ReadBundledNex<AbilityNexEntry>(language, "Ability.json", new AbilityNexCatalogReader()));

        var jobCommands = JoinJobCommand(
            ReadBundledNex<JobCommandNexEntry>(language, "JobCommand.json", new JobCommandNexCatalogReader()));
        var statusEffects = JoinStatusEffect(
            ReadBundledNex<StatusEffectNexEntry>(language, "UIStatusEffect.json", new StatusEffectNexCatalogReader()));
        var charaNames = JoinCharaName(
            ReadBundledNex<CharaNameNexEntry>(language, "CharaName.json", new CharaNameNexCatalogReader()));

        return BuildContext(language, GameDataSource.UserOverride, tablesDirectory,
            jobs, items, abilities, jobCommands, statusEffects, charaNames);
    }

    public GameDataContext LoadWithFallback(string? tablesDirectory, string language = "en")
    {
        if (!string.IsNullOrEmpty(tablesDirectory))
        {
            try
            {
                return LoadUserOverride(tablesDirectory, language);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    $"GameDataLoader: user override at '{tablesDirectory}' failed to load " +
                    $"({ex.GetType().Name}: {ex.Message}). Falling back to bundled.");
            }
        }
        return LoadBundled(language);
    }

    // === Resource helpers ===

    private IReadOnlyList<T> ReadBundledXml<T>(string filename, dynamic reader)
    {
        var resourceName = $"{ResourceNamespace}.{ModloaderResourceFolder}.{filename}";
        using var stream = _resourceAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Bundled resource '{resourceName}' not found in assembly " +
                $"'{_resourceAssembly.FullName}'. This is a build error.");
        return (IReadOnlyList<T>)reader.Read(stream);
    }

    private IReadOnlyList<T> ReadFileXml<T>(string path, dynamic reader)
    {
        using var stream = File.OpenRead(path);
        return (IReadOnlyList<T>)reader.Read(stream);
    }

    private IReadOnlyList<T> ReadBundledNex<T>(string language, string filename, dynamic reader)
    {
        var resourceName = $"{ResourceNamespace}.{NexResourceFolder}.{language}.{filename}";
        using var stream = _resourceAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            _logger.LogWarning(
                $"GameDataLoader: Nex catalog '{filename}' for language '{language}' not found " +
                $"(resource '{resourceName}'). Names will fall back to 'Unknown ... (ID n)'.");
            return Array.Empty<T>();
        }
        return (IReadOnlyList<T>)reader.Read(stream);
    }

    // === Joiners (one per table) ===

    private static IReadOnlyList<JobInfo> JoinJob(
        IReadOnlyList<JobDataXmlEntry> xml,
        IReadOnlyList<JobNexEntry> nex)
    {
        var nexById = nex.ToDictionary(e => e.Id);
        var result = new List<JobInfo>(xml.Count);
        foreach (var x in xml)
        {
            string name = string.Empty, description = string.Empty;
            int jobTypeId = 0, jobCommandId = 0;
            if (nexById.TryGetValue(x.Id, out var n))
            {
                name = n.Name; description = n.Description;
                jobTypeId = n.JobTypeId; jobCommandId = n.JobCommandId;
            }
            result.Add(new JobInfo(x.Id, name, description, jobTypeId, jobCommandId,
                x.HpGrowth, x.HpMultiplier, x.MpGrowth, x.MpMultiplier,
                x.SpeedGrowth, x.SpeedMultiplier, x.PaGrowth, x.PaMultiplier,
                x.MaGrowth, x.MaMultiplier, x.Move, x.Jump, x.CharacterEvasion));
        }
        return result;
    }

    private static IReadOnlyList<ItemInfo> JoinItem(
        IReadOnlyList<ItemDataXmlEntry> xml,
        IReadOnlyList<ItemNexEntry> nex)
    {
        var nexById = nex.ToDictionary(e => e.Id);
        var result = new List<ItemInfo>(xml.Count);
        foreach (var x in xml)
        {
            string name = string.Empty, description = string.Empty;
            string nameSingular = string.Empty, namePlural = string.Empty;
            if (nexById.TryGetValue(x.Id, out var n))
            {
                name = n.Name; description = n.Description;
                nameSingular = n.NameSingular; namePlural = n.NamePlural;
            }
            result.Add(new ItemInfo(x.Id, name, description, nameSingular, namePlural,
                x.ItemCategory, x.Price, x.RequiredLevel));
        }
        return result;
    }

    private static IReadOnlyList<AbilityInfo> JoinAbility(
        IReadOnlyList<AbilityDataXmlEntry> xml,
        IReadOnlyList<AbilityNexEntry> nex)
    {
        var nexById = nex.ToDictionary(e => e.Id);
        var result = new List<AbilityInfo>(xml.Count);
        foreach (var x in xml)
        {
            string name = string.Empty, description = string.Empty;
            int jpCost = 0;
            if (nexById.TryGetValue(x.Id, out var n))
            {
                name = n.Name; description = n.Description; jpCost = n.JpCost;
            }
            result.Add(new AbilityInfo(x.Id, name, description, jpCost,
                x.ChanceToLearn, x.AbilityType));
        }
        return result;
    }

    private static IReadOnlyList<JobCommandInfo> JoinJobCommand(IReadOnlyList<JobCommandNexEntry> nex)
        => nex.Select(n => new JobCommandInfo(n.Id, n.Name, n.Description)).ToList();

    private static IReadOnlyList<StatusEffectInfo> JoinStatusEffect(IReadOnlyList<StatusEffectNexEntry> nex)
        => nex.Select(n => new StatusEffectInfo(n.Id, n.Name, n.Description, n.Type)).ToList();

    private static IReadOnlyList<CharacterNameInfo> JoinCharaName(IReadOnlyList<CharaNameNexEntry> nex)
        => nex.Select(n => new CharacterNameInfo(n.NameNo, n.Name, n.IsGeneric)).ToList();

    private static GameDataContext BuildContext(
        string language,
        GameDataSource source,
        string sourcePath,
        IReadOnlyList<JobInfo> jobs,
        IReadOnlyList<ItemInfo> items,
        IReadOnlyList<AbilityInfo> abilities,
        IReadOnlyList<JobCommandInfo> jobCommands,
        IReadOnlyList<StatusEffectInfo> statusEffects,
        IReadOnlyList<CharacterNameInfo> characterNames)
    {
        return new GameDataContext(
            language: language,
            source: source,
            sourcePath: sourcePath,
            jobs: new JobDataTable(jobs),
            items: new ItemDataTable(items),
            abilities: new AbilityDataTable(abilities),
            jobCommands: new JobCommandDataTable(jobCommands),
            statusEffects: new StatusEffectDataTable(statusEffects),
            characterNames: new CharacterNameTable(characterNames));
    }
}

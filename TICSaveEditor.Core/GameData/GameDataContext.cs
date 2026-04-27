namespace TICSaveEditor.Core.GameData;

public class GameDataContext
{
    private readonly JobDataTable _jobs;
    private readonly ItemDataTable _items;
    private readonly AbilityDataTable _abilities;
    private readonly JobCommandDataTable _jobCommands;
    private readonly StatusEffectDataTable _statusEffects;
    private readonly CharacterNameTable _characterNames;

    internal GameDataContext(
        string language,
        GameDataSource source,
        string sourcePath,
        JobDataTable jobs,
        ItemDataTable items,
        AbilityDataTable abilities,
        JobCommandDataTable jobCommands,
        StatusEffectDataTable statusEffects,
        CharacterNameTable characterNames)
    {
        Language = language;
        Source = source;
        SourcePath = sourcePath;
        _jobs = jobs;
        _items = items;
        _abilities = abilities;
        _jobCommands = jobCommands;
        _statusEffects = statusEffects;
        _characterNames = characterNames;
    }

    public string Language { get; }
    public GameDataSource Source { get; }
    public string SourcePath { get; }

    public IReadOnlyList<JobInfo> Jobs => _jobs.Entries;
    public IReadOnlyList<ItemInfo> Items => _items.Entries;
    public IReadOnlyList<AbilityInfo> Abilities => _abilities.Entries;
    public IReadOnlyList<JobCommandInfo> JobCommands => _jobCommands.Entries;
    public IReadOnlyList<StatusEffectInfo> StatusEffects => _statusEffects.Entries;
    public IReadOnlyList<CharacterNameInfo> CharacterNames => _characterNames.Entries;

    public string GetJobName(int jobId) => _jobs.GetName(jobId);
    public string GetItemName(int itemId) => _items.GetName(itemId);
    public string GetAbilityName(int abilityId) => _abilities.GetName(abilityId);
    public string GetCommandName(int commandId) => _jobCommands.GetName(commandId);
    public string GetStatusEffectName(int statusId) => _statusEffects.GetName(statusId);
    public string GetCharacterName(ushort nameNo) => _characterNames.GetName(nameNo);

    public bool TryGetJob(int jobId, out JobInfo? info) => _jobs.TryGet(jobId, out info);
    public bool TryGetItem(int itemId, out ItemInfo? info) => _items.TryGet(itemId, out info);
    public bool TryGetAbility(int abilityId, out AbilityInfo? info) => _abilities.TryGet(abilityId, out info);
    public bool TryGetCommand(int commandId, out JobCommandInfo? info) => _jobCommands.TryGet(commandId, out info);
    public bool TryGetStatusEffect(int statusId, out StatusEffectInfo? info) => _statusEffects.TryGet(statusId, out info);
    public bool TryGetCharacterName(ushort nameNo, out CharacterNameInfo? info) => _characterNames.TryGet(nameNo, out info);
}

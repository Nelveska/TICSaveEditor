namespace TICSaveEditor.Core.GameData.Nex;

internal record JobNexEntry(
    int Id,
    string Name,
    string Description,
    int JobTypeId,
    int JobCommandId);

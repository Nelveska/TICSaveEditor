namespace TICSaveEditor.Core.GameData;

public record AbilityInfo(
    int Id,
    string Name,
    string Description,
    int JpCost,
    byte ChanceToLearn,
    string AbilityType);

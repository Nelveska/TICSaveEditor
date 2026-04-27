namespace TICSaveEditor.Core.GameData;

internal interface IGameDataLogger
{
    void LogWarning(string message);

    void LogError(string message, Exception? exception = null);
}

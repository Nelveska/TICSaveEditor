namespace TICSaveEditor.Core.GameData;

internal sealed class NullGameDataLogger : IGameDataLogger
{
    public static readonly NullGameDataLogger Instance = new();

    private NullGameDataLogger() { }

    public void LogWarning(string message) { }

    public void LogError(string message, Exception? exception = null) { }
}

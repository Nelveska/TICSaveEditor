namespace TICSaveEditor.Core.GameData;

public static class BundledGameData
{
    public const string ModloaderVersion = "1.7.0";
    public const string ModloaderCopiedAt = "2026-04-25";

    public const string NexLayoutsRepo = "skeewirt/fftivc-nex-layouts";
    public const string NexLayoutsCommit = "335747e";
    public const string NexLayoutsCopiedAt = "2026-04-26";

    public const string Ff16ToolsVersion = "1.13.0";

    public static readonly IReadOnlyList<string> BundledLanguages
        = new[] { "en", "fr", "ja", "de" };
}

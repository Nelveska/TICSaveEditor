using TICSaveEditor.Core.GameData;

namespace TICSaveEditor.GUI.Tests;

/// <summary>
/// xUnit class fixture: load <see cref="GameDataContext"/> once per test class to
/// amortize the ~tens-of-ms XML+JSON parsing cost across tests in the class.
/// </summary>
public sealed class GameDataFixture
{
    public GameDataFixture()
    {
        Context = new GameDataLoader().LoadWithFallback(tablesDirectory: null, language: "en");
    }

    public GameDataContext Context { get; }
}

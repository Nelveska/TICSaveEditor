using System;
using System.IO;

namespace TICSaveEditor.GUI.Tests;

/// <summary>
/// Locates the repo's <c>SaveFiles/</c> fixtures by walking up from the test
/// binary directory. Five baseline fixtures from the M5.5 SaveDiff star pattern
/// (per <c>decisions_umif_realfixture_locations.md</c>).
/// </summary>
internal static class SaveFixturePaths
{
    public static readonly string[] FixtureNames =
    {
        "Baseline",
        "EquipSet",
        "InternalChecksum",
        "Inventory",
        "JobChange",
    };

    public static string FixturesRoot { get; } = LocateFixturesRoot();

    public static string Enhanced(string fixtureName)
        => Path.Combine(FixturesRoot, fixtureName, "enhanced.png");

    private static string LocateFixturesRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "SaveFiles");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            $"Could not locate SaveFiles/ relative to {AppContext.BaseDirectory}");
    }
}

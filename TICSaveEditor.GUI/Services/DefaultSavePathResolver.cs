using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TICSaveEditor.GUI.Services;

/// <summary>
/// Resolves the default save-folder path at app boot. Per
/// <c>decisions_m10_default_path_descent.md</c>: Windows-only auto-detect under
/// <c>%USERPROFILE%/Documents/My Games/FINAL FANTASY TACTICS - The Ivalice Chronicles/Steam/</c>;
/// auto-descend a single steamID subfolder; multiple subfolders → return the parent;
/// non-Windows or missing → null. Browse button is always available regardless.
/// </summary>
public static class DefaultSavePathResolver
{
    private const string GameFolder = "FINAL FANTASY TACTICS - The Ivalice Chronicles";
    private const string SteamSubfolder = "Steam";

    public static string? TryGetDefault()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
        if (string.IsNullOrEmpty(userProfile))
        {
            return null;
        }

        var steamDir = Path.Combine(
            userProfile, "Documents", "My Games", GameFolder, SteamSubfolder);

        if (!Directory.Exists(steamDir))
        {
            return null;
        }

        string[] subdirs;
        try
        {
            subdirs = Directory.GetDirectories(steamDir);
        }
        catch
        {
            return steamDir;
        }

        return subdirs.Length switch
        {
            1 => subdirs[0],
            _ => steamDir,
        };
    }
}

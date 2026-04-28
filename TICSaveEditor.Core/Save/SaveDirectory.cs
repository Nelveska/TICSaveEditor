using System.Buffers.Binary;
using System.Diagnostics;

namespace TICSaveEditor.Core.Save;

public class SaveDirectory
{
    private static readonly string[] GameProcessNames = { "FFT_enhanced", "FFT_classic" };
    private const string SteamCloudFile = "steam_autocloud.vdf";
    private const string BackupFolderName = ".editor-backups";

    private SaveDirectory(string path, bool isGameRunning, IReadOnlyList<SaveFileInfo> files)
    {
        Path = path;
        IsGameRunning = isGameRunning;
        Files = files;
    }

    public string Path { get; }
    public bool IsGameRunning { get; }
    public IReadOnlyList<SaveFileInfo> Files { get; }

    public static SaveDirectory Scan(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Save directory not found: {path}");
        }

        var isGameRunning = IsAnyGameProcessRunning();
        var files = new List<SaveFileInfo>();

        foreach (var filePath in Directory.GetFiles(path))
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            if (string.Equals(fileName, SteamCloudFile, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var info = TryProbe(filePath);
            if (info is not null)
            {
                files.Add(info);
            }
        }

        return new SaveDirectory(path, isGameRunning, files);
    }

    private static SaveFileInfo? TryProbe(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var kind = PeekKind(filePath);
            if (kind is null)
            {
                return null;
            }

            var fileName = fileInfo.Name;
            return new SaveFileInfo(
                Path: filePath,
                FileName: fileName,
                Kind: kind.Value,
                IsEditable: kind != SaveFileKind.ResumeBattle,
                LastWriteTime: fileInfo.LastWriteTimeUtc,
                Size: fileInfo.Length,
                IsNameClashRename: IsNameClashFilename(fileName));
        }
        catch
        {
            return null;
        }
    }

    private static SaveFileKind? PeekKind(string filePath)
    {
        try
        {
            byte[] payload;
            if (filePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = File.ReadAllBytes(filePath);
                var ffTo = PngEnvelope.Extract(bytes);
                // Multi-file UMIF (numFiles > 1) is the in-battle auto-save history
                // container that v0.1 cannot unpack. For vanilla TIC its only known
                // use is autoenhanced.png — infer ResumeBattle and short-circuit
                // before Unpack would throw NotSupportedException.
                if (UmifContainer.PeekFileCount(ffTo) > 1)
                {
                    return SaveFileKind.ResumeBattle;
                }
                payload = UmifContainer.Unpack(ffTo);
            }
            else
            {
                var buffer = new byte[0x20];
                using var fs = File.OpenRead(filePath);
                var read = fs.Read(buffer, 0, buffer.Length);
                if (read < 0x10)
                {
                    return null;
                }
                payload = read == buffer.Length ? buffer : buffer.AsSpan(0, read).ToArray();
            }

            return ProbeFromPayload(payload);
        }
        catch
        {
            return null;
        }
    }

    private static SaveFileKind? ProbeFromPayload(byte[] payload)
    {
        if (payload.Length < 0x10)
        {
            return null;
        }
        var discriminator = BinaryPrimitives.ReadUInt64LittleEndian(payload.AsSpan(0x08, 8));
        if (discriminator == 0x10ul)
        {
            if (payload.Length < 0x20)
            {
                return null;
            }
            var saveType = BinaryPrimitives.ReadUInt32LittleEndian(payload.AsSpan(0x1C, 4));
            return saveType switch
            {
                0u => SaveFileKind.ResumeWorld,
                1u => SaveFileKind.ResumeBattle,
                _ => null,
            };
        }
        return SaveFileKind.Manual;
    }

    private static bool IsAnyGameProcessRunning()
    {
        foreach (var name in GameProcessNames)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0)
                {
                    return true;
                }
            }
            catch
            {
            }
        }
        return false;
    }

    private static bool IsNameClashFilename(string fileName)
        => fileName.Contains("Name clash", StringComparison.OrdinalIgnoreCase);
}

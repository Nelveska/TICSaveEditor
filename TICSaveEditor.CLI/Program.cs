using TICSaveEditor.Core.GameData;
using TICSaveEditor.Core.Save;

namespace TICSaveEditor.CLI;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage(Console.Out);
            return 0;
        }

        return args[0] switch
        {
            "version" => PrintVersion(),
            "info" => PrintInfo(args),
            _ => UnknownCommand(args[0]),
        };
    }

    private static int PrintVersion()
    {
        Console.WriteLine("TICSaveEditor (v0.1 pre-release)");
        Console.WriteLine(
            $"Modloader bundle: {BundledGameData.ModloaderVersion} (copied {BundledGameData.ModloaderCopiedAt})");
        Console.WriteLine(
            $"Nex layouts: {BundledGameData.NexLayoutsRepo} @ {BundledGameData.NexLayoutsCommit} (copied {BundledGameData.NexLayoutsCopiedAt})");
        Console.WriteLine($"FF16Tools: {BundledGameData.Ff16ToolsVersion}");
        Console.WriteLine($"Bundled languages: {string.Join(", ", BundledGameData.BundledLanguages)}");
        return 0;
    }

    private static int PrintInfo(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: TICSaveEditor.CLI info <path>");
            return 2;
        }

        var path = args[1];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 1;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            var save = SaveFileLoader.Load(bytes, path);

            Console.WriteLine($"Kind: {save.Kind}");
            Console.WriteLine($"Source: {save.SourcePath}");
            Console.WriteLine($"File size: {bytes.Length} bytes");
            Console.WriteLine($"Stored CRC: 0x{save.StoredChecksum:X8}");
            Console.WriteLine($"Format discriminator: 0x{save.FormatDiscriminator:X16}");

            switch (save)
            {
                case ManualSaveFile m:
                    var nonEmpty = m.Slots.Count(s => !s.IsEmpty);
                    Console.WriteLine($"Slots: {m.Slots.Count} ({nonEmpty} non-empty)");
                    var shown = 0;
                    foreach (var slot in m.Slots)
                    {
                        if (slot.IsEmpty) continue;
                        Console.WriteLine(
                            $"  [{slot.Index}] Title=\"{slot.SlotTitle}\" "
                            + $"Difficulty={slot.SaveWork.FftoConfig.DifficultyLevel}");
                        if (++shown >= 5) break;
                    }
                    break;
                case ResumeWorldSaveFile rw:
                    Console.WriteLine($"FFTI SaveType: {rw.FftiHeader.SaveType} (world)");
                    Console.WriteLine($"FFTI region size: {rw.FftiHeader.RawBytes.Length} bytes");
                    break;
                case ResumeBattleSaveFile rb:
                    Console.WriteLine($"FFTI SaveType: {rb.FftiHeader.SaveType} (battle)");
                    Console.WriteLine($"Raw payload: {rb.RawPayload.Length} bytes");
                    Console.WriteLine("Note: in-battle saves are not editable in this version.");
                    break;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int PrintUsage(TextWriter writer)
    {
        writer.WriteLine("Usage: TICSaveEditor.CLI <command>");
        writer.WriteLine();
        writer.WriteLine("Commands:");
        writer.WriteLine("  version          Print bundled-data version info");
        writer.WriteLine("  info <path>      Print save-file info (kind, slots, CRC)");
        return 0;
    }

    private static int UnknownCommand(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        PrintUsage(Console.Error);
        return 2;
    }
}

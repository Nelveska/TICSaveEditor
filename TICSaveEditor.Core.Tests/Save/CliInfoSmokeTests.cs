using System.Diagnostics;
using TICSaveEditor.Core.Tests.Fixtures;

namespace TICSaveEditor.Core.Tests.Save;

public class CliInfoSmokeTests : IDisposable
{
    private readonly string _tempDir;

    public CliInfoSmokeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tic-cli-smoke-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { }
    }

    [Fact]
    public void Info_manual_save_prints_kind_and_slots()
    {
        var path = Path.Combine(_tempDir, "manual.png");
        File.WriteAllBytes(path, SyntheticSaveBuilder.BuildManualSavePng());

        var (stdout, exitCode) = RunCli("info", path);

        Assert.Equal(0, exitCode);
        Assert.Contains("Kind: Manual", stdout);
        Assert.Contains("Slots: 50", stdout);
        Assert.Contains($"Title=\"{SyntheticSaveBuilder.Slot0Title}\"", stdout);
        Assert.Contains($"Difficulty={SyntheticSaveBuilder.Slot0DifficultyLevel}", stdout);
    }

    [Fact]
    public void Info_resume_world_save_prints_savetype_zero()
    {
        var path = Path.Combine(_tempDir, "world.sav");
        File.WriteAllBytes(path, SyntheticSaveBuilder.BuildResumeWorldSaveRaw());

        var (stdout, exitCode) = RunCli("info", path);

        Assert.Equal(0, exitCode);
        Assert.Contains("Kind: ResumeWorld", stdout);
        Assert.Contains("FFTI SaveType: 0", stdout);
    }

    [Fact]
    public void Info_resume_battle_save_prints_savetype_one_and_not_editable_note()
    {
        var path = Path.Combine(_tempDir, "battle.sav");
        File.WriteAllBytes(path, SyntheticSaveBuilder.BuildResumeBattleSaveRaw());

        var (stdout, exitCode) = RunCli("info", path);

        Assert.Equal(0, exitCode);
        Assert.Contains("Kind: ResumeBattle", stdout);
        Assert.Contains("FFTI SaveType: 1", stdout);
        Assert.Contains("not editable", stdout);
    }

    private static (string Stdout, int ExitCode) RunCli(params string[] args)
    {
        var dllPath = LocateCliDll();
        var psi = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(dllPath);
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet for CLI smoke test.");
        var stdout = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(10_000);
        return (stdout, proc.ExitCode);
    }

    private static string LocateCliDll()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "TICSaveEditor.sln")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        if (dir is null)
        {
            throw new FileNotFoundException("Could not locate solution root from test bin path.");
        }
        var candidate = Path.Combine(
            dir, "TICSaveEditor.CLI", "bin", "Debug", "net8.0", "TICSaveEditor.CLI.dll");
        if (!File.Exists(candidate))
        {
            throw new FileNotFoundException(
                $"CLI dll not found at expected path: {candidate}. Run 'dotnet build' first.");
        }
        return candidate;
    }
}

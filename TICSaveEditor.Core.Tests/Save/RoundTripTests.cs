using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Tests.Fixtures;

namespace TICSaveEditor.Core.Tests.Save;

public class RoundTripTests : IDisposable
{
    private readonly string _tempDir;

    public RoundTripTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tic-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { }
    }

    [Fact]
    public void Manual_save_raw_round_trips_byte_identical()
    {
        var input = SyntheticSaveBuilder.BuildManualSaveRaw();
        var output = RoundTrip(input, "save.sav");
        Assert.Equal(input, output);
    }

    [Fact]
    public void Manual_save_png_round_trips_byte_identical()
    {
        var input = SyntheticSaveBuilder.BuildManualSavePng();
        var output = RoundTrip(input, "save.png");
        Assert.Equal(input, output);
    }

    [Fact]
    public void Resume_world_save_raw_round_trips_byte_identical()
    {
        var input = SyntheticSaveBuilder.BuildResumeWorldSaveRaw();
        var output = RoundTrip(input, "resume.sav");
        Assert.Equal(input, output);
    }

    [Fact]
    public void Resume_world_save_png_round_trips_byte_identical()
    {
        var input = SyntheticSaveBuilder.BuildResumeWorldSavePng();
        var output = RoundTrip(input, "resume.png");
        Assert.Equal(input, output);
    }

    private byte[] RoundTrip(byte[] input, string fileName)
    {
        var path = Path.Combine(_tempDir, fileName);
        var save = SaveFileLoader.Load(input, path);
        save.SaveAs(path);
        return File.ReadAllBytes(path);
    }
}

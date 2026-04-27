using TICSaveEditor.Core.Save;
using TICSaveEditor.Core.Tests.Fixtures;

namespace TICSaveEditor.Core.Tests.Save;

public class SaveFileLoaderTests
{
    [Fact]
    public void Loads_manual_save_raw()
    {
        var bytes = SyntheticSaveBuilder.BuildManualSaveRaw();
        var save = SaveFileLoader.Load(bytes, "test.sav");

        var manual = Assert.IsType<ManualSaveFile>(save);
        Assert.Equal(SaveFileKind.Manual, manual.Kind);
        Assert.Equal(50, manual.Slots.Count);
        Assert.False(manual.Slots[0].IsEmpty);
        Assert.True(manual.Slots[1].IsEmpty);
    }

    [Fact]
    public void Loads_manual_save_png()
    {
        var bytes = SyntheticSaveBuilder.BuildManualSavePng();
        var save = SaveFileLoader.Load(bytes, "test.png");

        Assert.IsType<ManualSaveFile>(save);
        Assert.Equal(SaveFileKind.Manual, save.Kind);
    }

    [Fact]
    public void Loads_resume_world_save_raw()
    {
        var bytes = SyntheticSaveBuilder.BuildResumeWorldSaveRaw();
        var save = SaveFileLoader.Load(bytes, "test.sav");

        var resume = Assert.IsType<ResumeWorldSaveFile>(save);
        Assert.Equal(SaveFileKind.ResumeWorld, resume.Kind);
        Assert.Equal(FftiHeader.MagicValue, resume.FftiHeader.Magic);
        Assert.Equal(0u, resume.FftiHeader.SaveType);
        Assert.Equal(SaveWork.Size, resume.SaveWork.RawBytes.Length);
    }

    [Fact]
    public void Loads_resume_battle_save_raw()
    {
        var bytes = SyntheticSaveBuilder.BuildResumeBattleSaveRaw();
        var save = SaveFileLoader.Load(bytes, "test.sav");

        var resume = Assert.IsType<ResumeBattleSaveFile>(save);
        Assert.Equal(SaveFileKind.ResumeBattle, resume.Kind);
        Assert.Equal(1u, resume.FftiHeader.SaveType);
        Assert.NotEmpty(resume.RawPayload);
    }

    [Fact]
    public void Resume_battle_save_throws_on_save()
    {
        var bytes = SyntheticSaveBuilder.BuildResumeBattleSaveRaw();
        var save = SaveFileLoader.Load(bytes, "test.sav");
        Assert.Throws<NotSupportedException>(() => save.Save());
        Assert.Throws<NotSupportedException>(() => save.SaveAs("anywhere.sav"));
    }

    [Fact]
    public void Loads_resume_world_png()
    {
        var bytes = SyntheticSaveBuilder.BuildResumeWorldSavePng();
        var save = SaveFileLoader.Load(bytes, "test.png");
        Assert.IsType<ResumeWorldSaveFile>(save);
    }

    [Fact]
    public void Throws_on_too_small_payload()
    {
        var tiny = new byte[8];
        Assert.Throws<InvalidDataException>(() => SaveFileLoader.Load(tiny, "test.sav"));
    }

    [Fact]
    public void Throws_on_bad_png_signature()
    {
        var bogus = new byte[64];
        Assert.Throws<InvalidDataException>(() => SaveFileLoader.Load(bogus, "test.png"));
    }

    [Fact]
    public void Stored_checksum_is_read_from_payload()
    {
        var bytes = SyntheticSaveBuilder.BuildManualSaveRaw();
        var save = SaveFileLoader.Load(bytes, "test.sav");
        Assert.NotEqual(0u, save.StoredChecksum);
    }

    [Fact]
    public void Format_discriminator_distinguishes_manual_from_resume()
    {
        var manual = SaveFileLoader.Load(SyntheticSaveBuilder.BuildManualSaveRaw(), "m.sav");
        var resume = SaveFileLoader.Load(SyntheticSaveBuilder.BuildResumeWorldSaveRaw(), "r.sav");
        Assert.NotEqual(0x10ul, manual.FormatDiscriminator);
        Assert.Equal(0x10ul, resume.FormatDiscriminator);
    }
}

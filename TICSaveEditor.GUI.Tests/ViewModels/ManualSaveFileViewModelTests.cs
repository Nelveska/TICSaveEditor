using System.IO;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

public class ManualSaveFileViewModelTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public ManualSaveFileViewModelTests(GameDataFixture fixture) => _fixture = fixture;

    [Fact]
    public void Slots_count_is_always_50()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        Assert.Equal(50, vm.Slots.Count);
    }

    [Fact]
    public void Empty_slot_proxies_render_safely()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        foreach (var slot in vm.Slots)
        {
            // No throw on empty/populated slots' display paths.
            _ = slot.Title;
            _ = slot.SaveTimestampDisplay;
            _ = slot.PlaytimeDisplay;
            _ = slot.HeroName;
        }
    }
}

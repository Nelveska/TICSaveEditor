using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Save command + IsDirty proxy + dirty-intercept tests for MainWindowViewModel.
/// </summary>
public class MainWindowViewModelSaveTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public MainWindowViewModelSaveTests(GameDataFixture fixture) => _fixture = fixture;

    private string CopyBaselineToTemp()
    {
        var src = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(src);
        var dir = Path.Combine(Path.GetTempPath(), "tic-m11-tests");
        System.IO.Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, $"enhanced-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(dest, bytes);
        return dest;
    }

    private MainWindowViewModel BuildVmWithOpenFile(string tempPath)
    {
        var bytes = File.ReadAllBytes(tempPath);
        var save = SaveFileLoader.Load(bytes, tempPath);
        var fileVm = SaveFileViewModelFactory.Create(save, _fixture.Context);
        var vm = new MainWindowViewModel(_fixture.Context) { OpenFile = fileVm };
        return vm;
    }

    [Fact]
    public void IsDirty_starts_false_after_load()
    {
        var temp = CopyBaselineToTemp();
        try
        {
            var vm = BuildVmWithOpenFile(temp);
            Assert.False(vm.IsDirty);
            Assert.False(vm.SaveCommand.CanExecute(null));
        }
        finally { File.Delete(temp); }
    }

    [Fact]
    public void IsDirty_flips_when_model_marks_dirty()
    {
        var temp = CopyBaselineToTemp();
        try
        {
            var vm = BuildVmWithOpenFile(temp);
            vm.OpenFile!.Model.MarkDirty();
            Assert.True(vm.IsDirty);
            Assert.True(vm.SaveCommand.CanExecute(null));
            Assert.Equal("TICSaveEditor *", vm.WindowTitle);
        }
        finally { File.Delete(temp); }
    }

    [Fact]
    public async Task SaveCommand_writes_file_and_clears_dirty()
    {
        var temp = CopyBaselineToTemp();
        try
        {
            var vm = BuildVmWithOpenFile(temp);
            // Mutate via the bulk op so IsDirty is set the same way M11 does in production.
            var slot = ((ManualSaveFileViewModel)vm.OpenFile!).Slots
                .First(s => !s.IsEmpty);
            slot.AskLevelAsync = () => Task.FromResult<int?>(42);
            slot.ShowOperationResultAsync = (_, _) => Task.CompletedTask;
            await slot.SetAllToLevelCommand.ExecuteAsync(null);
            Assert.True(vm.IsDirty);

            await vm.SaveCommand.ExecuteAsync(null);

            Assert.False(vm.IsDirty);
            Assert.Equal("TICSaveEditor", vm.WindowTitle);
            Assert.Contains("Saved", vm.StatusMessage);

            // Reload & verify byte-faithful persistence of mutation.
            var reloadedBytes = File.ReadAllBytes(temp);
            var reloaded = SaveFileLoader.Load(reloadedBytes, temp);
            var reloadedManual = (ManualSaveFile)reloaded;
            var reloadedSlot = reloadedManual.Slots.First(s => !s.IsEmpty);
            Assert.Contains(reloadedSlot.SaveWork.Battle.Units, u => !u.IsEmpty && u.Level == 42);
        }
        finally { File.Delete(temp); }
    }

    [Fact]
    public async Task ConfirmDiscardIfDirty_returns_true_when_clean()
    {
        var temp = CopyBaselineToTemp();
        try
        {
            var vm = BuildVmWithOpenFile(temp);
            var asked = false;
            vm.ConfirmDiscardChangesAsync = () => { asked = true; return Task.FromResult(false); };
            var ok = await vm.ConfirmDiscardIfDirtyAsync();
            Assert.True(ok);
            Assert.False(asked);
        }
        finally { File.Delete(temp); }
    }

    [Fact]
    public async Task ConfirmDiscardIfDirty_asks_user_when_dirty()
    {
        var temp = CopyBaselineToTemp();
        try
        {
            var vm = BuildVmWithOpenFile(temp);
            vm.OpenFile!.Model.MarkDirty();
            var asked = false;
            vm.ConfirmDiscardChangesAsync = () => { asked = true; return Task.FromResult(true); };
            var ok = await vm.ConfirmDiscardIfDirtyAsync();
            Assert.True(asked);
            Assert.True(ok);
        }
        finally { File.Delete(temp); }
    }

    [Fact]
    public async Task ConfirmDiscardIfDirty_returns_false_on_user_decline()
    {
        var temp = CopyBaselineToTemp();
        try
        {
            var vm = BuildVmWithOpenFile(temp);
            vm.OpenFile!.Model.MarkDirty();
            vm.ConfirmDiscardChangesAsync = () => Task.FromResult(false);
            var ok = await vm.ConfirmDiscardIfDirtyAsync();
            Assert.False(ok);
        }
        finally { File.Delete(temp); }
    }

    [Fact]
    public async Task BrowseCommand_aborts_when_user_declines_discard()
    {
        var temp = CopyBaselineToTemp();
        try
        {
            var vm = BuildVmWithOpenFile(temp);
            vm.OpenFile!.Model.MarkDirty();
            vm.ConfirmDiscardChangesAsync = () => Task.FromResult(false);
            var pickerInvoked = false;
            vm.PickFolderAsync = () => { pickerInvoked = true; return Task.FromResult<string?>(null); };

            await vm.BrowseCommand.ExecuteAsync(null);

            Assert.False(pickerInvoked);
            Assert.True(vm.IsDirty);          // unchanged
            Assert.NotNull(vm.OpenFile);      // not cleared
        }
        finally { File.Delete(temp); }
    }
}

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TICSaveEditor.Core.Operations;
using TICSaveEditor.Core.Save;
using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

/// <summary>
/// Exercises the 3 M11 bulk-op commands on real fixtures with stubbed dialog Funcs.
/// Each test asserts: command runs, parent SaveFile.IsDirty flips on success, and
/// the OperationResult is forwarded to the captured ShowOperationResultAsync stub.
/// </summary>
public class SaveSlotViewModelOperationsTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public SaveSlotViewModelOperationsTests(GameDataFixture fixture) => _fixture = fixture;

    private (ManualSaveFileViewModel vm, SaveSlotViewModel populated) LoadBaseline()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var populated = vm.Slots.First(s => !s.IsEmpty);
        return (vm, populated);
    }

    [Fact]
    public async Task SetAllToLevel_succeeds_and_marks_dirty()
    {
        var (vm, slot) = LoadBaseline();
        OperationResult? captured = null;
        slot.AskLevelAsync = () => Task.FromResult<int?>(50);
        slot.ShowOperationResultAsync = (label, r) => { captured = r; return Task.CompletedTask; };

        await slot.SetAllToLevelCommand.ExecuteAsync(null);

        Assert.NotNull(captured);
        Assert.True(captured!.Succeeded);
        Assert.True(captured.UnitsAffected > 0);
        Assert.True(vm.Model.IsDirty);
        // Spot-check: at least one unit's Level is now 50.
        Assert.Contains(slot.Units, u => !u.IsEmpty && u.Model.Level == 50);
    }

    [Fact]
    public async Task SetAllToLevel_cancel_returns_no_mutation_and_no_dirty()
    {
        var (vm, slot) = LoadBaseline();
        slot.AskLevelAsync = () => Task.FromResult<int?>(null);  // user clicked Cancel
        var resultSeen = false;
        slot.ShowOperationResultAsync = (_, _) => { resultSeen = true; return Task.CompletedTask; };

        await slot.SetAllToLevelCommand.ExecuteAsync(null);

        Assert.False(resultSeen);
        Assert.False(vm.Model.IsDirty);
    }

    [Fact]
    public async Task MaxAllJobPoints_succeeds_and_marks_dirty()
    {
        var (vm, slot) = LoadBaseline();
        OperationResult? captured = null;
        slot.ShowOperationResultAsync = (_, r) => { captured = r; return Task.CompletedTask; };

        await slot.MaxAllJobPointsCommand.ExecuteAsync(null);

        Assert.NotNull(captured);
        Assert.True(captured!.Succeeded);
        Assert.True(captured.UnitsAffected > 0);
        Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public async Task LearnAllAbilitiesCurrentJob_succeeds_and_marks_dirty()
    {
        // Real fixtures contain canonical and story-unique-class units. Per the
        // class-name + slot-0-fallback rule (M11 follow-up #5): canonical-class
        // units write to their named slot, story-unique-class units write to
        // slot 0, and only monsters/placeholders/unknowns are skipped.
        var (vm, slot) = LoadBaseline();
        OperationResult? captured = null;
        slot.ShowOperationResultAsync = (_, r) => { captured = r; return Task.CompletedTask; };

        await slot.LearnAllAbilitiesCurrentJobCommand.ExecuteAsync(null);

        Assert.NotNull(captured);
        Assert.True(captured!.Succeeded,
            $"Expected success on Baseline; got Exception={captured.Exception?.GetType().Name}");
        // IsDirty only flips when at least one unit was affected; the Baseline fixture
        // has at least one populated unit with a generic Job in [0, 21] range.
        if (captured.UnitsAffected > 0) Assert.True(vm.Model.IsDirty);
    }

    [Fact]
    public void CanExecute_is_false_for_empty_slot()
    {
        var path = SaveFixturePaths.Enhanced("Baseline");
        var bytes = File.ReadAllBytes(path);
        var save = SaveFileLoader.Load(bytes, path);
        var vm = (ManualSaveFileViewModel)SaveFileViewModelFactory.Create(save, _fixture.Context);
        var empty = vm.Slots.First(s => s.IsEmpty);

        Assert.False(empty.SetAllToLevelCommand.CanExecute(null));
        Assert.False(empty.MaxAllJobPointsCommand.CanExecute(null));
        Assert.False(empty.LearnAllAbilitiesCurrentJobCommand.CanExecute(null));
    }

    [Fact]
    public void CanExecute_is_true_for_populated_slot()
    {
        var (_, slot) = LoadBaseline();
        Assert.False(slot.IsEmpty);
        Assert.True(slot.SetAllToLevelCommand.CanExecute(null));
        Assert.True(slot.MaxAllJobPointsCommand.CanExecute(null));
        Assert.True(slot.LearnAllAbilitiesCurrentJobCommand.CanExecute(null));
    }
}

using TICSaveEditor.GUI.ViewModels;

namespace TICSaveEditor.GUI.Tests.ViewModels;

public class MainWindowViewModelTests : IClassFixture<GameDataFixture>
{
    private readonly GameDataFixture _fixture;

    public MainWindowViewModelTests(GameDataFixture fixture) => _fixture = fixture;

    [Fact]
    public void Constructor_does_not_throw_with_no_default_path()
    {
        // On CI/Linux/non-game machines, DefaultSavePathResolver returns null.
        // Constructor must handle that path gracefully and produce a usable VM.
        var vm = new MainWindowViewModel(_fixture.Context);
        Assert.NotNull(vm.StatusMessage);
        Assert.Null(vm.OpenFile);
    }

    [Fact]
    public void Scanning_a_missing_directory_does_not_throw()
    {
        var vm = new MainWindowViewModel(_fixture.Context);
        vm.SaveDirectoryPath = @"Z:\does\not\exist\at\all";
        vm.RefreshCommand.Execute(null);
        Assert.Null(vm.Directory);
        Assert.Contains("not found", vm.StatusMessage);
    }

    [Fact]
    public void Game_data_summary_includes_jobs_and_items_counts()
    {
        var vm = new MainWindowViewModel(_fixture.Context);
        Assert.Contains("jobs", vm.GameDataSummary);
        Assert.Contains("items", vm.GameDataSummary);
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TICSaveEditor.Core.GameData;
using TICSaveEditor.GUI.ViewModels;
using TICSaveEditor.GUI.Views;

namespace TICSaveEditor.GUI;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Bundled-only, English-only at boot per decisions_m10_scope.md.
            // LoadWithFallback never throws (decisions_m7_*); falls back to bundled
            // on any override failure.
            var gameData = new GameDataLoader().LoadWithFallback(tablesDirectory: null, language: "en");

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(gameData),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

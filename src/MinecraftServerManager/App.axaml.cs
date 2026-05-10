using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MinecraftServerManager.Services;
using MinecraftServerManager.ViewModels;
using MinecraftServerManager.Views;

namespace MinecraftServerManager;

public partial class App : Application
{
    private readonly InstanceRegistry _registry = new();
    private readonly ProcessSupervisor _supervisor = new();
    private readonly PapermcApi _papermc = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _registry.Load();

        if (Current != null)
        {
            Current.RequestedThemeVariant = _registry.Settings.UseDarkTheme
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(_registry, _supervisor, _papermc)
            };

            desktop.Exit += (_, _) =>
            {
                _supervisor.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

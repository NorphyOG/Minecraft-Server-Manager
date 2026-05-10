using System.Collections.ObjectModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftServerManager.Models;
using MinecraftServerManager.Services;

namespace MinecraftServerManager.ViewModels;

public partial class QuickSetupViewModel : ViewModelBase
{
    private readonly InstanceRegistry _registry;
    private readonly PapermcApi _api;
    private Window? _dialogWindow;

    public QuickSetupViewModel(InstanceRegistry registry, PapermcApi api)
    {
        _registry = registry;
        _api = api;
        _ = LoadVersionsAsync();
    }

    public void AttachWindow(Window window) => _dialogWindow = window;

    [ObservableProperty]
    private string serverName = "Mein Paper Server";

    [ObservableProperty]
    private string minecraftVersion = "1.21.4";

    [ObservableProperty]
    private string jvmArgs = "";

    [ObservableProperty]
    private int serverPort = 25565;

    [ObservableProperty]
    private string levelName = "world";

    [ObservableProperty]
    private bool eulaAccepted;

    /// <summary>Anlegen nur möglich nach EULA-Bestätigung.</summary>
    public bool IsCreateEnabled => EulaAccepted;

    partial void OnEulaAcceptedChanged(bool value) => OnPropertyChanged(nameof(IsCreateEnabled));

    [ObservableProperty]
    private bool enableRcon = true;

    [ObservableProperty]
    private int rconPort = 25575;

    [ObservableProperty]
    private string rconPassword = "changeme";

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private string statusText = "";

    public ObservableCollection<string> PaperVersions { get; } = new();

    private async Task LoadVersionsAsync()
    {
        try
        {
            var v = await _api.GetPaperVersionsAsync();
            PaperVersions.Clear();
            foreach (var s in v.OrderDescending().Take(40))
                PaperVersions.Add(s);
            if (PaperVersions.Count > 0)
                MinecraftVersion = PaperVersions[0];
        }
        catch (Exception ex)
        {
            StatusText = "Versionen konnten nicht geladen werden: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task Create()
    {
        if (!EulaAccepted)
        {
            StatusText = "Bitte die Minecraft EULA bestätigen (siehe https://aka.ms/MinecraftEULA).";
            return;
        }

        try
        {
            StatusText = "Lade Paper-Build…";
            var buildInfo = await _api.GetLatestBuildAsync(MinecraftVersion);
            if (buildInfo == null)
            {
                StatusText = "Kein Build für diese Version gefunden.";
                return;
            }

            var (build, jarName) = buildInfo.Value;
            var root = _registry.Settings.ServersRootDirectory;
            Directory.CreateDirectory(root);
            var safeName = string.Join("_", ServerName.Split(Path.GetInvalidFileNameChars()));
            var dir = Path.Combine(root, safeName);
            if (Directory.Exists(dir))
            {
                StatusText = "Ordner existiert bereits. Bitte anderen Namen wählen.";
                return;
            }

            Directory.CreateDirectory(dir);

            var jarPath = Path.Combine(dir, "server.jar");
            var progress = new Progress<double>(p => DownloadProgress = p);
            await _api.DownloadPaperJarAsync(MinecraftVersion, build, jarName, jarPath, progress, CancellationToken.None);

            var instance = new ServerInstance
            {
                Name = ServerName,
                Loader = ServerLoaderKind.Paper,
                MinecraftVersion = MinecraftVersion,
                BuildNumber = build,
                DirectoryPath = dir,
                JarFileName = "server.jar",
                JvmArguments = string.IsNullOrWhiteSpace(JvmArgs) ? _registry.Settings.DefaultJvmArguments : JvmArgs.Trim(),
                ServerPort = ServerPort,
                LevelName = LevelName,
                EulaAccepted = true,
                RconEnabled = EnableRcon,
                RconPort = RconPort,
                RconPassword = RconPassword,
                Tags = ServerTag.None
            };

            ServerFilesWriter.WriteServerProperties(instance);
            ServerFilesWriter.WriteEula(instance);
            _registry.UpsertInstance(instance);

            StatusText = "Server angelegt.";
            _dialogWindow?.Close();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void Cancel() => _dialogWindow?.Close();
}

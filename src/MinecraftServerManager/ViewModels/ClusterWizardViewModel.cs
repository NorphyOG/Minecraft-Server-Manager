using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftServerManager.Models;
using MinecraftServerManager.Services;

namespace MinecraftServerManager.ViewModels;

public partial class ClusterWizardViewModel : ViewModelBase
{
    private readonly InstanceRegistry _registry;
    private readonly PapermcApi _api;
    private Window? _dialogWindow;

    public ClusterWizardViewModel(InstanceRegistry registry, PapermcApi api)
    {
        _registry = registry;
        _api = api;
        _ = LoadListsAsync();
    }

    public void AttachWindow(Window window) => _dialogWindow = window;

    [ObservableProperty]
    private string clusterDisplayName = "Haupt-Netzwerk";

    [ObservableProperty]
    private string selectedMcVersion = "1.21.4";

    [ObservableProperty]
    private string selectedVelocityVersion = "";

    [ObservableProperty]
    private int proxyPort = 25565;

    [ObservableProperty]
    private string backendLobbyName = "lobby";

    [ObservableProperty]
    private int backendLobbyPort = 25566;

    [ObservableProperty]
    private string backendGameName = "survival";

    [ObservableProperty]
    private int backendGamePort = 25567;

    [ObservableProperty]
    private double downloadProgress;

    [ObservableProperty]
    private string statusText = "";

    public ObservableCollection<string> PaperVersions { get; } = new();

    public ObservableCollection<string> VelocityVersions { get; } = new();

    private async Task LoadListsAsync()
    {
        try
        {
            var pv = await _api.GetPaperVersionsAsync();
            PaperVersions.Clear();
            foreach (var s in pv.OrderDescending().Take(40))
                PaperVersions.Add(s);

            var vv = await _api.GetVelocityVersionsAsync();
            VelocityVersions.Clear();
            foreach (var s in vv.OrderDescending().Take(20))
                VelocityVersions.Add(s);

            if (PaperVersions.Count > 0)
                SelectedMcVersion = PaperVersions[0];
            if (VelocityVersions.Count > 0)
                SelectedVelocityVersion = VelocityVersions[0];
        }
        catch (Exception ex)
        {
            StatusText = "API-Fehler: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task CreateCluster(Window window)
    {
        try
        {
            StatusText = "Erzeuge Cluster…";
            var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

            var root = _registry.Settings.ServersRootDirectory;
            Directory.CreateDirectory(root);

            var slug = string.Join("_", ClusterDisplayName.Split(Path.GetInvalidFileNameChars()));
            var proxyDir = Path.Combine(root, $"{slug}-proxy");
            var lobbyDir = Path.Combine(root, $"{slug}-{BackendLobbyName}");
            var gameDir = Path.Combine(root, $"{slug}-{BackendGameName}");

            foreach (var d in new[] { proxyDir, lobbyDir, gameDir })
            {
                if (Directory.Exists(d))
                {
                    StatusText = $"Ordner existiert bereits: {d}";
                    return;
                }
            }

            Directory.CreateDirectory(proxyDir);
            Directory.CreateDirectory(lobbyDir);
            Directory.CreateDirectory(gameDir);

            // Velocity
            StatusText = "Lade Velocity…";
            var vb = await _api.GetLatestVelocityBuildAsync(SelectedVelocityVersion);
            if (vb == null)
            {
                StatusText = "Kein Velocity-Build gefunden.";
                return;
            }

            var (vBuild, vJar) = vb.Value;
            await _api.DownloadVelocityJarAsync(
                SelectedVelocityVersion,
                vBuild,
                vJar,
                Path.Combine(proxyDir, "server.jar"),
                new Progress<double>(p => DownloadProgress = p),
                CancellationToken.None);

            var proxyInstance = new ServerInstance
            {
                Name = $"{ClusterDisplayName} (Proxy)",
                Loader = ServerLoaderKind.Velocity,
                MinecraftVersion = SelectedVelocityVersion,
                BuildNumber = vBuild,
                DirectoryPath = proxyDir,
                JarFileName = "server.jar",
                JvmArguments = _registry.Settings.DefaultJvmArguments,
                ServerPort = ProxyPort,
                LevelName = "world",
                EulaAccepted = true,
                AppendNogui = false,
                Tags = ServerTag.Proxy,
                ForwardingSecret = secret,
                RconEnabled = false
            };

            // Paper backends
            StatusText = "Lade Paper (Lobby)…";
            var pbLobby = await _api.GetLatestBuildAsync(SelectedMcVersion);
            if (pbLobby == null)
            {
                StatusText = "Kein Paper-Build für Lobby.";
                return;
            }

            var (pBuild1, pJar1) = pbLobby.Value;
            await _api.DownloadPaperJarAsync(
                SelectedMcVersion,
                pBuild1,
                pJar1,
                Path.Combine(lobbyDir, "server.jar"),
                new Progress<double>(p => DownloadProgress = p),
                CancellationToken.None);

            StatusText = "Lade Paper (Spiel)…";
            var pbGame = await _api.GetLatestBuildAsync(SelectedMcVersion);
            if (pbGame == null)
            {
                StatusText = "Kein Paper-Build für Spielserver.";
                return;
            }

            var (pBuild2, pJar2) = pbGame.Value;
            await _api.DownloadPaperJarAsync(
                SelectedMcVersion,
                pBuild2,
                pJar2,
                Path.Combine(gameDir, "server.jar"),
                new Progress<double>(p => DownloadProgress = p),
                CancellationToken.None);

            var lobbyInstance = new ServerInstance
            {
                Name = $"{ClusterDisplayName} · {BackendLobbyName}",
                Loader = ServerLoaderKind.Paper,
                MinecraftVersion = SelectedMcVersion,
                BuildNumber = pBuild1,
                DirectoryPath = lobbyDir,
                JarFileName = "server.jar",
                JvmArguments = _registry.Settings.DefaultJvmArguments,
                ServerPort = BackendLobbyPort,
                LevelName = "world",
                EulaAccepted = true,
                Tags = ServerTag.Backend | ServerTag.Lobby,
                ForwardingSecret = secret,
                RconEnabled = true,
                RconPort = BackendLobbyPort + 100,
                RconPassword = "changeme"
            };

            var gameInstance = new ServerInstance
            {
                Name = $"{ClusterDisplayName} · {BackendGameName}",
                Loader = ServerLoaderKind.Paper,
                MinecraftVersion = SelectedMcVersion,
                BuildNumber = pBuild2,
                DirectoryPath = gameDir,
                JarFileName = "server.jar",
                JvmArguments = _registry.Settings.DefaultJvmArguments,
                ServerPort = BackendGamePort,
                LevelName = "world",
                EulaAccepted = true,
                Tags = ServerTag.Backend,
                ForwardingSecret = secret,
                RconEnabled = true,
                RconPort = BackendGamePort + 100,
                RconPassword = "changeme"
            };

            var clusterId = Guid.NewGuid();
            proxyInstance.ClusterId = clusterId;
            lobbyInstance.ClusterId = clusterId;
            gameInstance.ClusterId = clusterId;

            ServerFilesWriter.WriteServerProperties(proxyInstance);
            ServerFilesWriter.WriteEula(proxyInstance);

            ServerFilesWriter.WriteServerProperties(lobbyInstance);
            ServerFilesWriter.WriteEula(lobbyInstance);
            ServerFilesWriter.EnsurePaperVelocityForwarding(lobbyDir, secret);

            ServerFilesWriter.WriteServerProperties(gameInstance);
            ServerFilesWriter.WriteEula(gameInstance);
            ServerFilesWriter.EnsurePaperVelocityForwarding(gameDir, secret);

            var backends = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [SanitizeServerKey(BackendLobbyName)] = $"127.0.0.1:{BackendLobbyPort}",
                [SanitizeServerKey(BackendGameName)] = $"127.0.0.1:{BackendGamePort}"
            };

            ClusterConfigWriter.WriteVelocityToml(
                proxyInstance,
                backends,
                SanitizeServerKey(BackendLobbyName));

            _registry.UpsertInstance(proxyInstance);
            _registry.UpsertInstance(lobbyInstance);
            _registry.UpsertInstance(gameInstance);

            _registry.UpsertCluster(new ClusterDefinition
            {
                Id = clusterId,
                DisplayName = ClusterDisplayName,
                ProxyInstanceId = proxyInstance.Id,
                BackendInstanceIds = [lobbyInstance.Id, gameInstance.Id],
                UseVelocity = true
            });

            StatusText = "Cluster erstellt.";
            _dialogWindow?.Close();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    private static string SanitizeServerKey(string name)
    {
        var s = string.Join("", name.Where(char.IsLetterOrDigit));
        return string.IsNullOrEmpty(s) ? "server" : s.ToLowerInvariant();
    }

    [RelayCommand]
    private void Cancel() => _dialogWindow?.Close();
}

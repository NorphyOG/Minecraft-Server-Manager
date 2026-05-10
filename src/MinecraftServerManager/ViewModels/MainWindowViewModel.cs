using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinecraftServerManager.Models;
using MinecraftServerManager.Services;
using MinecraftServerManager.Views;

namespace MinecraftServerManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly InstanceRegistry _registry;
    private readonly ProcessSupervisor _supervisor;
    private readonly PapermcApi _papermc;
    private readonly CommandPresetsService _presets = new();

    private readonly Dictionary<Guid, ObservableCollection<string>> _consoles = new();

    public MainWindowViewModel(InstanceRegistry registry, ProcessSupervisor supervisor, PapermcApi papermc)
    {
        _registry = registry;
        _supervisor = supervisor;
        _papermc = papermc;

        LoadPresetsAsset();
        _registry.StateChanged += (_, _) => Dispatcher.UIThread.Post(RefreshAll);
        _supervisor.ProcessExited += OnProcessExited;

        LoadSettingsFields();
        RefreshAll();
    }

    private void LoadSettingsFields()
    {
        PreferredJavaPath = _registry.Settings.PreferredJavaPath ?? "";
        ArtifactCacheDirectory = _registry.Settings.ArtifactCacheDirectory;
        ServersRootDirectory = _registry.Settings.ServersRootDirectory;
        DefaultJvmArguments = _registry.Settings.DefaultJvmArguments;
        UseDarkTheme = _registry.Settings.UseDarkTheme;
    }

    private void LoadPresetsAsset()
    {
        try
        {
            var uri = new Uri("avares://MinecraftServerManager/Assets/command-presets.json");
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            _presets.LoadFromStream(stream);
        }
        catch
        {
            /* ignored — presets optional */
        }
    }

    public ObservableCollection<SidebarRow> SidebarRows { get; } = new();

    public ObservableCollection<ServerInstance> Instances { get; } = new();

    [ObservableProperty]
    private MainPane currentPane = MainPane.Dashboard;

    [ObservableProperty]
    private ServerInstance? selectedInstance;

    [ObservableProperty]
    private ObservableCollection<string> currentConsole = new();

    [ObservableProperty]
    private string serverPropertiesText = "";

    [ObservableProperty]
    private string consoleInput = "";

    [ObservableProperty]
    private string rconInput = "";

    [ObservableProperty]
    private string statusMessage = "";

    /// <summary>True when mindestens eine Server-Instanz registriert ist (Sidebar &amp; Dashboard).</summary>
    [ObservableProperty]
    private bool hasInstances;

    [ObservableProperty]
    private string automationBeforePath = "";

    [ObservableProperty]
    private string automationAfterPath = "";

    [ObservableProperty]
    private string clusterSummary = "";

    /// <summary>Eine Zeile für die Übersichtskarte (Cluster-Kurzinfo).</summary>
    [ObservableProperty]
    private string overviewClusterLine = "";

    /// <summary>Kurzinfo Bind-Adresse aus server.properties (Textfeld).</summary>
    [ObservableProperty]
    private string networkBindSummary = "";

    /// <summary>Für Zwischenablage: z. B. 127.0.0.1:25565 (Port aus Instanz).</summary>
    [ObservableProperty]
    private string quickConnectionText = "";

    /// <summary>Hinweis wenn server-ip leer oder 0.0.0.0 — andere Clients nutzen LAN/öffentliche IP.</summary>
    [ObservableProperty]
    private string quickConnectionHint = "";

    [ObservableProperty]
    private bool isClusterMember;

    [ObservableProperty]
    private string pluginsFolderCountLabel = "";

    [ObservableProperty]
    private string modsFolderCountLabel = "";

    [ObservableProperty]
    private string rconTestResult = "";

    [ObservableProperty]
    private string diagnoseTail = "";

    [ObservableProperty]
    private string pluginsFolderPath = "";

    [ObservableProperty]
    private string modsFolderPath = "";

    [ObservableProperty]
    private string extensionsGuidance = "";

    [ObservableProperty]
    private string loaderMismatchHint = "";

    [ObservableProperty]
    private int detailTabIndex;

    [ObservableProperty]
    private string preferredJavaPath = "";

    [ObservableProperty]
    private string artifactCacheDirectory = "";

    [ObservableProperty]
    private string serversRootDirectory = "";

    [ObservableProperty]
    private string defaultJvmArguments = "";

    [ObservableProperty]
    private bool useDarkTheme = true;

    public ObservableCollection<PresetEntry> PresetCommands { get; } = new();

    partial void OnSelectedInstanceChanged(ServerInstance? value)
    {
        PresetCommands.Clear();
        if (value == null)
        {
            CurrentPane = MainPane.Dashboard;
            ServerPropertiesText = "";
            AutomationBeforePath = "";
            AutomationAfterPath = "";
            ClusterSummary = "";
            OverviewClusterLine = "";
            NetworkBindSummary = "";
            QuickConnectionText = "";
            QuickConnectionHint = "";
            IsClusterMember = false;
            PluginsFolderCountLabel = "";
            ModsFolderCountLabel = "";
            RconTestResult = "";
            DiagnoseTail = "";
            PluginsFolderPath = "";
            ModsFolderPath = "";
            ExtensionsGuidance = "";
            LoaderMismatchHint = "";
            return;
        }

        CurrentPane = MainPane.ServerDetail;
        LoadServerProperties(value);
        RefreshNetworkDisplay();
        AutomationBeforePath = value.OnBeforeStartScriptPath ?? "";
        AutomationAfterPath = value.OnAfterStopScriptPath ?? "";
        RefreshClusterSummary(value);
        BindConsole(value.Id);
        RefreshDiagnose();
        RefreshExtensions();

        foreach (var p in _presets.GetPresets(value.Loader))
            PresetCommands.Add(new PresetEntry(p.Label, p.Command));
    }

    private void RefreshExtensions()
    {
        if (SelectedInstance == null)
            return;

        var dir = SelectedInstance.DirectoryPath;
        PluginsFolderPath = ServerExtensionsInfo.PluginsPath(dir);
        ModsFolderPath = ServerExtensionsInfo.ModsPath(dir);
        PluginsFolderCountLabel = FormatFolderFileCount(PluginsFolderPath, "plugins");
        ModsFolderCountLabel = FormatFolderFileCount(ModsFolderPath, "mods");
        var cluster = SelectedInstance.ClusterId is { } cid ? _registry.FindCluster(cid) : null;
        ExtensionsGuidance = ServerExtensionsInfo.BuildGuidance(SelectedInstance, cluster);
        LoaderMismatchHint = ServerExtensionsInfo.LoaderMismatchHint(SelectedInstance) ?? "";
    }

    private static string FormatFolderFileCount(string path, string shortLabel)
    {
        if (!Directory.Exists(path))
            return $"{shortLabel}/: Ordner noch nicht vorhanden.";
        var n = Directory.GetFiles(path, "*.jar").Length;
        return $"{shortLabel}/: {n} *.jar (nur oberste Ebene).";
    }

    partial void OnAutomationBeforePathChanged(string value)
    {
        if (SelectedInstance == null)
            return;
        SelectedInstance.OnBeforeStartScriptPath = string.IsNullOrWhiteSpace(value) ? null : value;
        _registry.UpsertInstance(SelectedInstance);
    }

    partial void OnAutomationAfterPathChanged(string value)
    {
        if (SelectedInstance == null)
            return;
        SelectedInstance.OnAfterStopScriptPath = string.IsNullOrWhiteSpace(value) ? null : value;
        _registry.UpsertInstance(SelectedInstance);
    }

    private void RefreshClusterSummary(ServerInstance value)
    {
        var c = _registry.ClusterForInstance(value.Id);
        if (c == null)
        {
            IsClusterMember = false;
            ClusterSummary = "Diese Instanz ist keinem Cluster zugeordnet.";
            OverviewClusterLine = "Keine Cluster-Zuordnung";
            return;
        }

        IsClusterMember = true;
        var proxy = _registry.Find(c.ProxyInstanceId);
        var backs = c.BackendInstanceIds.Select(id => _registry.Find(id)).Where(x => x != null).ToList();
        ClusterSummary =
            $"Cluster: {c.DisplayName}\nProxy: {proxy?.Name ?? "?"}\nBackends: {string.Join(", ", backs.Select(b => b!.Name))}";
        OverviewClusterLine = $"Cluster: {c.DisplayName} (Proxy: {proxy?.Name ?? "?"})";
    }

    /// <summary>Beispielskript für Tab Automation (nur Anzeige).</summary>
    public string AutomationExampleScript =>
        "# Vor Start / nach Stopp — verfügbare Umgebungsvariablen (MSM_*):\r\n" +
        "#   MSM_INSTANCE_DIR, MSM_INSTANCE_ID, MSM_INSTANCE_NAME,\r\n" +
        "#   MSM_PORT, MSM_LOADER, MSM_MC_VERSION\r\n" +
        "\r\n" +
        "Write-Host \"Instanzordner: $env:MSM_INSTANCE_DIR\"\r\n" +
        "Write-Host \"Port: $env:MSM_PORT | Loader: $env:MSM_LOADER | MC: $env:MSM_MC_VERSION\"\r\n" +
        "\r\n" +
        "# Beispiel: Backup-Ordner mit Datum\r\n" +
        "# $dest = Join-Path $env:MSM_INSTANCE_DIR (\"backup-\" + (Get-Date -Format yyyyMMdd-HHmm))\r\n" +
        "# Copy-Item -Recurse (Join-Path $env:MSM_INSTANCE_DIR \"world\") $dest\r\n";

    partial void OnServerPropertiesTextChanged(string value)
    {
        if (SelectedInstance != null)
            RefreshNetworkDisplay();
    }

    private void RefreshNetworkDisplay()
    {
        if (SelectedInstance == null)
            return;

        var ip = ParseJavaProperty(ServerPropertiesText, "server-ip");
        QuickConnectionText = $"127.0.0.1:{SelectedInstance.ServerPort}";
        NetworkBindSummary = string.IsNullOrWhiteSpace(ip)
            ? "Bind: (leer = alle Schnittstellen, siehe server-ip)"
            : $"Bind: {ip}";

        var bindAll = string.IsNullOrWhiteSpace(ip)
                      || string.Equals(ip, "0.0.0.0", StringComparison.OrdinalIgnoreCase);
        QuickConnectionHint = bindAll
            ? "Hinweis: Der Server lauscht auf allen Schnittstellen (server-ip leer oder 0.0.0.0). Andere Spieler im LAN oder aus dem Internet verbinden sich mit der LAN- bzw. öffentlichen IP dieses Rechners und diesem Port. „Adresse kopieren“ liefert 127.0.0.1 nur für lokale Tests auf diesem PC."
            : "";
    }

    private static string? ParseJavaProperty(string text, string key)
    {
        foreach (var raw in text.ReplaceLineEndings("\n").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            var k = line[..eq].Trim();
            if (!string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                continue;
            return line[(eq + 1)..].Trim();
        }

        return null;
    }

    private async Task CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        var w = GetMainWindow();
        if (w?.Clipboard != null)
            await w.Clipboard.SetTextAsync(text);
        StatusMessage = "In Zwischenablage kopiert.";
    }

    private void BindConsole(Guid id)
    {
        if (!_consoles.TryGetValue(id, out var lines))
        {
            lines = new ObservableCollection<string>();
            _consoles[id] = lines;
        }

        CurrentConsole = lines;
    }

    private void OnProcessExited(Guid id)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var inst = _registry.Find(id);
            if (inst != null)
            {
                inst.RuntimeStatus = InstanceStatus.Stopped;
                StatusMessage = $"Prozess beendet: {inst.Name}";
            }
        });
    }

    private void RefreshAll()
    {
        Instances.Clear();
        foreach (var i in _registry.Instances.OrderBy(x => x.Name))
            Instances.Add(i);

        SidebarRows.Clear();
        var standalone = _registry.Instances.Where(i => i.ClusterId == null).OrderBy(x => x.Name).ToList();
        if (standalone.Count > 0)
        {
            SidebarRows.Add(SidebarRow.Section("Einzelserver"));
            foreach (var s in standalone)
                SidebarRows.Add(SidebarRow.Server(s));
        }

        foreach (var cluster in _registry.Clusters.OrderBy(c => c.DisplayName))
        {
            SidebarRows.Add(SidebarRow.Section($"Cluster · {cluster.DisplayName}"));
            var proxy = _registry.Find(cluster.ProxyInstanceId);
            if (proxy != null)
                SidebarRows.Add(SidebarRow.Server(proxy));
            foreach (var bid in cluster.BackendInstanceIds)
            {
                var b = _registry.Find(bid);
                if (b != null)
                    SidebarRows.Add(SidebarRow.Server(b));
            }
        }

        var orphans = _registry.Instances
            .Where(i => i.ClusterId != null && _registry.FindCluster(i.ClusterId.Value) == null)
            .OrderBy(x => x.Name)
            .ToList();

        if (orphans.Count > 0)
        {
            SidebarRows.Add(SidebarRow.Section("Ohne gültigen Cluster"));
            foreach (var o in orphans)
                SidebarRows.Add(SidebarRow.Server(o));
        }

        HasInstances = Instances.Count > 0;
    }

    private void LoadServerProperties(ServerInstance instance)
    {
        var path = Path.Combine(instance.DirectoryPath, "server.properties");
        ServerPropertiesText = File.Exists(path) ? File.ReadAllText(path) : "";
    }

    [RelayCommand]
    private void SelectDashboard()
    {
        SelectedInstance = null;
        CurrentPane = MainPane.Dashboard;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SelectedInstance = null;
        CurrentPane = MainPane.Settings;
    }

    [RelayCommand]
    private void ApplyPreset(PresetEntry? entry)
    {
        if (entry == null)
            return;
        ConsoleInput = entry.Command;
    }

    [RelayCommand]
    private void SelectServer(ServerInstance? instance)
    {
        if (instance == null)
            return;
        SelectedInstance = instance;
    }

    [RelayCommand]
    private void SaveAppSettings()
    {
        _registry.UpdateSettings(s =>
        {
            s.PreferredJavaPath = string.IsNullOrWhiteSpace(PreferredJavaPath) ? null : PreferredJavaPath.Trim();
            s.ArtifactCacheDirectory = ArtifactCacheDirectory.Trim();
            s.ServersRootDirectory = ServersRootDirectory.Trim();
            s.DefaultJvmArguments = DefaultJvmArguments.Trim();
            s.UseDarkTheme = UseDarkTheme;
        });
        StatusMessage = "Einstellungen gespeichert.";
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = UseDarkTheme ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light;
    }

    [RelayCommand]
    private async Task StartServer()
    {
        if (SelectedInstance == null)
            return;

        var inst = SelectedInstance;
        inst.LastError = null;

        if (PortChecker.IsTcpPortInUse(inst.ServerPort) && !_supervisor.IsRunning(inst.Id))
        {
            inst.RuntimeStatus = InstanceStatus.PortConflict;
            StatusMessage = $"Port {inst.ServerPort} ist bereits belegt.";
            return;
        }

        if (!inst.EulaAccepted)
        {
            StatusMessage = "EULA wurde nicht bestätigt.";
            return;
        }

        var java = JavaLocator.ResolveJavaExecutable(_registry.Settings, inst);

        if (!string.IsNullOrWhiteSpace(inst.OnBeforeStartScriptPath))
        {
            var env = ScriptRunner.BuildEnv(inst);
            var (code, output) = await ScriptRunner.RunHookAsync(inst.OnBeforeStartScriptPath!, env, CancellationToken.None);
            AppendLine(inst.Id, $"[hook vor Start exit={code}] {output}");
            if (code != 0)
            {
                StatusMessage = "Skript vor Start fehlgeschlagen.";
                return;
            }
        }

        ServerFilesWriter.WriteServerProperties(inst);
        ServerFilesWriter.WriteEula(inst);

        inst.RuntimeStatus = InstanceStatus.Starting;
        StatusMessage = $"Starte {inst.Name}…";

        void Out(string line) => AppendLine(inst.Id, line);
        void Err(string line) => AppendLine(inst.Id, "[stderr] " + line);

        var (ok, err) = await _supervisor.StartAsync(inst, java, Out, Err, CancellationToken.None);
        if (!ok)
        {
            inst.RuntimeStatus = InstanceStatus.Error;
            inst.LastError = err;
            StatusMessage = err ?? "Start fehlgeschlagen.";
            return;
        }

        inst.RuntimeStatus = InstanceStatus.Running;
        StatusMessage = $"{inst.Name} läuft.";
    }

    [RelayCommand]
    private async Task StopServer()
    {
        if (SelectedInstance == null)
            return;
        StatusMessage = $"Stoppe {SelectedInstance.Name}…";
        await _supervisor.StopGracefullyAsync(SelectedInstance.Id, TimeSpan.FromSeconds(45), CancellationToken.None);
        SelectedInstance.RuntimeStatus = InstanceStatus.Stopped;

        if (!string.IsNullOrWhiteSpace(SelectedInstance.OnAfterStopScriptPath))
        {
            var env = ScriptRunner.BuildEnv(SelectedInstance);
            var (code, output) =
                await ScriptRunner.RunHookAsync(SelectedInstance.OnAfterStopScriptPath!, env, CancellationToken.None);
            AppendLine(SelectedInstance.Id, $"[hook nach Stopp exit={code}] {output}");
        }

        StatusMessage = $"{SelectedInstance.Name} gestoppt.";
    }

    [RelayCommand]
    private async Task StopAll()
    {
        StatusMessage = "Stoppe alle Server…";
        await _supervisor.StopAllAsync(TimeSpan.FromSeconds(45), CancellationToken.None);
        foreach (var i in _registry.Instances)
            i.RuntimeStatus = InstanceStatus.Stopped;
        StatusMessage = "Alle gestoppt.";
    }

    [RelayCommand]
    private void SendConsole()
    {
        if (SelectedInstance == null || string.IsNullOrWhiteSpace(ConsoleInput))
            return;
        var line = ConsoleInput.TrimEnd('\r', '\n');
        ConsoleInput = "";
        if (!_supervisor.SendCommand(SelectedInstance.Id, line))
            StatusMessage = "Konsole nicht verbunden.";
    }

    [RelayCommand]
    private async Task SendRcon()
    {
        if (SelectedInstance == null || string.IsNullOrWhiteSpace(RconInput))
            return;
        if (!SelectedInstance.RconEnabled)
        {
            StatusMessage = "RCON ist für diese Instanz deaktiviert.";
            return;
        }

        try
        {
            var (ok, resp) = await RconClient.SendAsync(
                "127.0.0.1",
                SelectedInstance.RconPort,
                SelectedInstance.RconPassword,
                RconInput.Trim(),
                TimeSpan.FromSeconds(5),
                CancellationToken.None);
            AppendLine(SelectedInstance.Id, ok ? $"[rcon] {resp}" : $"[rcon error] {resp}");
            if (ok)
                RconInput = "";
        }
        catch (Exception ex)
        {
            AppendLine(SelectedInstance.Id, "[rcon] " + ex.Message);
        }
    }

    [RelayCommand]
    private void SaveServerProperties()
    {
        if (SelectedInstance == null)
            return;
        Directory.CreateDirectory(SelectedInstance.DirectoryPath);
        var path = Path.Combine(SelectedInstance.DirectoryPath, "server.properties");
        File.WriteAllText(path, ServerPropertiesText);
        RefreshNetworkDisplay();
        StatusMessage = "server.properties gespeichert.";
    }

    [RelayCommand]
    private async Task CopyQuickConnection()
    {
        if (SelectedInstance == null)
            return;
        await CopyToClipboardAsync(QuickConnectionText);
    }

    [RelayCommand]
    private void ClearConsole()
    {
        if (SelectedInstance == null)
            return;
        CurrentConsole.Clear();
    }

    [RelayCommand]
    private async Task CopyLastConsoleLine()
    {
        if (CurrentConsole.Count == 0)
            return;
        await CopyToClipboardAsync(CurrentConsole[^1]);
    }

    [RelayCommand]
    private void OpenServerPropertiesExternal()
    {
        if (SelectedInstance == null)
            return;
        var path = Path.Combine(SelectedInstance.DirectoryPath, "server.properties");
        try
        {
            if (!File.Exists(path))
            {
                StatusMessage = "server.properties fehlt — zuerst speichern oder Datei anlegen.";
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenLogsFolder()
    {
        if (SelectedInstance == null)
            return;
        var logsPath = Path.Combine(SelectedInstance.DirectoryPath, "logs");
        try
        {
            Directory.CreateDirectory(logsPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = logsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task TestRcon()
    {
        if (SelectedInstance == null)
            return;
        if (!SelectedInstance.RconEnabled)
        {
            RconTestResult = "RCON ist in dieser Instanz nicht aktiviert (enable-rcon / gespeicherte Konfiguration).";
            StatusMessage = "RCON-Test nicht möglich.";
            return;
        }

        if (string.IsNullOrEmpty(SelectedInstance.RconPassword))
        {
            RconTestResult = "Kein RCON-Passwort hinterlegt.";
            StatusMessage = "RCON-Test nicht möglich.";
            return;
        }

        try
        {
            var (ok, resp) = await RconClient.SendAsync(
                "127.0.0.1",
                SelectedInstance.RconPort,
                SelectedInstance.RconPassword,
                "list",
                TimeSpan.FromSeconds(5),
                CancellationToken.None);
            RconTestResult = ok ? resp : resp;
            StatusMessage = ok ? "RCON-Test erfolgreich." : "RCON-Test fehlgeschlagen.";
        }
        catch (Exception ex)
        {
            RconTestResult = ex.Message;
            StatusMessage = "RCON-Test: Verbindungsfehler.";
        }
    }

    [RelayCommand]
    private async Task OpenExplorer()
    {
        await OpenExplorerForInstance(SelectedInstance);
    }

    [RelayCommand]
    private async Task OpenExplorerForInstance(ServerInstance? instance)
    {
        if (instance == null)
            return;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = instance.DirectoryPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void OpenConsoleForInstance(ServerInstance? instance)
    {
        if (instance == null)
            return;
        SelectedInstance = instance;
        DetailTabIndex = 1;
    }

    [RelayCommand]
    private async Task DeleteServer(ServerInstance? instance)
    {
        var target = instance ?? SelectedInstance;
        if (target == null)
            return;

        if (_supervisor.IsRunning(target.Id))
        {
            StatusMessage = "Server zuerst stoppen.";
            return;
        }

        var owner = GetMainWindow();
        if (owner == null)
            return;

        var message =
            $"„{target.Name}“ aus der Verwaltung entfernen?\n\n" +
            "Dateien im Ordner bleiben erhalten; nur der Eintrag in Minecraft Server Manager wird gelöscht.";
        var dlg = new ConfirmDialog(message, "Server entfernen");
        var ok = await dlg.ShowDialog<bool>(owner);
        if (!ok)
            return;

        var id = target.Id;
        var name = target.Name;
        _registry.RemoveInstance(id);
        _consoles.Remove(id);

        if (SelectedInstance?.Id == id)
            SelectedInstance = null;

        RefreshAll();
        StatusMessage = $"Server „{name}“ entfernt.";
    }

    [RelayCommand]
    private async Task OpenPluginsFolder()
    {
        if (SelectedInstance == null)
            return;
        try
        {
            var path = ServerExtensionsInfo.PluginsPath(SelectedInstance.DirectoryPath);
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenModsFolder()
    {
        if (SelectedInstance == null)
            return;
        try
        {
            var path = ServerExtensionsInfo.ModsPath(SelectedInstance.DirectoryPath);
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    private void EnsurePluginsFolder()
    {
        if (SelectedInstance == null)
            return;
        Directory.CreateDirectory(ServerExtensionsInfo.PluginsPath(SelectedInstance.DirectoryPath));
        RefreshExtensions();
        StatusMessage = "Ordner plugins/ wurde angelegt (falls er fehlte).";
    }

    [RelayCommand]
    private void EnsureModsFolder()
    {
        if (SelectedInstance == null)
            return;
        Directory.CreateDirectory(ServerExtensionsInfo.ModsPath(SelectedInstance.DirectoryPath));
        RefreshExtensions();
        StatusMessage = "Ordner mods/ wurde angelegt (falls er fehlte).";
    }

    [RelayCommand]
    private void RefreshDiagnose()
    {
        if (SelectedInstance == null)
        {
            DiagnoseTail = "";
            return;
        }

        var logPath = Path.Combine(SelectedInstance.DirectoryPath, "logs", "latest.log");
        if (!File.Exists(logPath))
        {
            DiagnoseTail = "Keine latest.log gefunden (Server noch nicht gestartet?).";
            return;
        }

        try
        {
            var lines = File.ReadAllLines(logPath);
            DiagnoseTail = string.Join(Environment.NewLine, lines.TakeLast(80));
        }
        catch (Exception ex)
        {
            DiagnoseTail = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenQuickSetup()
    {
        var owner = GetMainWindow();
        if (owner == null)
            return;
        var vm = new QuickSetupViewModel(_registry, _papermc);
        var w = new QuickSetupWindow { DataContext = vm };
        vm.AttachWindow(w);
        await w.ShowDialog(owner);
        RefreshAll();
    }

    [RelayCommand]
    private async Task OpenClusterWizard()
    {
        var owner = GetMainWindow();
        if (owner == null)
            return;
        var vm = new ClusterWizardViewModel(_registry, _papermc);
        var w = new ClusterWizardWindow { DataContext = vm };
        vm.AttachWindow(w);
        await w.ShowDialog(owner);
        RefreshAll();
        if (SelectedInstance != null)
            RefreshClusterSummary(SelectedInstance);
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime d)
            return d.MainWindow as Window;
        return null;
    }

    private void AppendLine(Guid id, string line)
    {
        if (!_consoles.TryGetValue(id, out var coll))
        {
            coll = new ObservableCollection<string>();
            _consoles[id] = coll;
        }

        Dispatcher.UIThread.Post(() =>
        {
            coll.Add(line);
            while (coll.Count > 4000)
                coll.RemoveAt(0);
            if (SelectedInstance?.Id == id)
                CurrentConsole = coll;
        });
    }

    public InstanceRegistry Registry => _registry;
}

public sealed record PresetEntry(string Label, string Command);

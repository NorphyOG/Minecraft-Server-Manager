using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace MinecraftServerManager.Models;

public sealed class ServerInstance : INotifyPropertyChanged
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Neuer Server";

    public ServerLoaderKind Loader { get; set; } = ServerLoaderKind.Paper;

    /// <summary>Game version string, e.g. 1.21.4</summary>
    public string MinecraftVersion { get; set; } = "1.21.4";

    /// <summary>Paper/Velocity build number from API when downloaded.</summary>
    public int? BuildNumber { get; set; }

    /// <summary>Absolute or relative path to server directory.</summary>
    public string DirectoryPath { get; set; } = "";

    /// <summary>File name of server jar inside DirectoryPath.</summary>
    public string JarFileName { get; set; } = "server.jar";

    /// <summary>Optional path to java.exe; empty = PATH.</summary>
    public string? JavaExecutablePath { get; set; }

    public string JvmArguments { get; set; } = "-Xms512M -Xmx2G";

    public int ServerPort { get; set; } = 25565;

    public string LevelName { get; set; } = "world";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ServerTag Tags { get; set; } = ServerTag.None;

    public Guid? ClusterId { get; set; }

    public bool EulaAccepted { get; set; }

    public bool RconEnabled { get; set; }

    public int RconPort { get; set; } = 25575;

    public string RconPassword { get; set; } = "";

    public string? OnBeforeStartScriptPath { get; set; }

    public string? OnAfterStopScriptPath { get; set; }

    /// <summary>Velocity modern forwarding secret when part of cluster.</summary>
    public string? ForwardingSecret { get; set; }

    /// <summary>Minecraft Java Edition Server nutzt <c>nogui</c>; Velocity nicht.</summary>
    public bool AppendNogui { get; set; } = true;

    private InstanceStatus _runtimeStatus = InstanceStatus.Stopped;

    private string? _lastError;

    [JsonIgnore]
    public InstanceStatus RuntimeStatus
    {
        get => _runtimeStatus;
        set => SetField(ref _runtimeStatus, value);
    }

    [JsonIgnore]
    public string? LastError
    {
        get => _lastError;
        set => SetField(ref _lastError, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

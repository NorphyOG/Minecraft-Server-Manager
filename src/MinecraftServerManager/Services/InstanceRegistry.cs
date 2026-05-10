using System.Text.Json;
using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

public sealed class InstanceRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private PersistedState _state = new();

    public IReadOnlyList<ServerInstance> Instances => _state.Instances;

    public IReadOnlyList<ClusterDefinition> Clusters => _state.Clusters;

    public AppSettings Settings => _state.Settings;

    public event EventHandler? StateChanged;

    public void Load()
    {
        Directory.CreateDirectory(PathsHelper.AppDataDirectory);
        var path = PathsHelper.StateFilePath;
        if (!File.Exists(path))
        {
            _state = new PersistedState();
            if (string.IsNullOrWhiteSpace(_state.Settings.ArtifactCacheDirectory))
                _state.Settings.ArtifactCacheDirectory = PathsHelper.DefaultArtifactCache();
            if (string.IsNullOrWhiteSpace(_state.Settings.ServersRootDirectory))
                _state.Settings.ServersRootDirectory = PathsHelper.DefaultServersRoot();
            Save();
            StateChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var json = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<PersistedState>(json, JsonOptions) ?? new PersistedState();
        _state = loaded;
        if (string.IsNullOrWhiteSpace(_state.Settings.ArtifactCacheDirectory))
            _state.Settings.ArtifactCacheDirectory = PathsHelper.DefaultArtifactCache();
        if (string.IsNullOrWhiteSpace(_state.Settings.ServersRootDirectory))
            _state.Settings.ServersRootDirectory = PathsHelper.DefaultServersRoot();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        PathsHelper.EnsureParent(PathsHelper.StateFilePath);
        var json = JsonSerializer.Serialize(_state, JsonOptions);
        File.WriteAllText(PathsHelper.StateFilePath, json);
    }

    public void UpsertInstance(ServerInstance instance)
    {
        var idx = _state.Instances.FindIndex(i => i.Id == instance.Id);
        if (idx >= 0)
            _state.Instances[idx] = instance;
        else
            _state.Instances.Add(instance);
        Save();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveInstance(Guid id)
    {
        _state.Instances.RemoveAll(i => i.Id == id);
        foreach (var c in _state.Clusters.ToList())
        {
            if (c.ProxyInstanceId == id || c.BackendInstanceIds.Contains(id))
            {
                c.BackendInstanceIds.Remove(id);
                if (c.ProxyInstanceId == id)
                    _state.Clusters.Remove(c);
            }
        }
        Save();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpsertCluster(ClusterDefinition cluster)
    {
        var idx = _state.Clusters.FindIndex(c => c.Id == cluster.Id);
        if (idx >= 0)
            _state.Clusters[idx] = cluster;
        else
            _state.Clusters.Add(cluster);
        Save();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public ServerInstance? Find(Guid id) => _state.Instances.FirstOrDefault(i => i.Id == id);

    public ClusterDefinition? FindCluster(Guid id) => _state.Clusters.FirstOrDefault(c => c.Id == id);

    public ClusterDefinition? ClusterForInstance(Guid instanceId) =>
        _state.Clusters.FirstOrDefault(c =>
            c.ProxyInstanceId == instanceId || c.BackendInstanceIds.Contains(instanceId));

    public void UpdateSettings(Action<AppSettings> mutate)
    {
        mutate(_state.Settings);
        Save();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

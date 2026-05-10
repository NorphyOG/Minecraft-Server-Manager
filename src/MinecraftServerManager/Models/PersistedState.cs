namespace MinecraftServerManager.Models;

public sealed class PersistedState
{
    public List<ServerInstance> Instances { get; set; } = [];

    public List<ClusterDefinition> Clusters { get; set; } = [];

    public AppSettings Settings { get; set; } = new();
}

namespace MinecraftServerManager.Models;

public sealed class ClusterDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DisplayName { get; set; } = "Neues Netzwerk";

    public Guid ProxyInstanceId { get; set; }

    public List<Guid> BackendInstanceIds { get; set; } = [];

    public bool UseVelocity { get; set; } = true;
}

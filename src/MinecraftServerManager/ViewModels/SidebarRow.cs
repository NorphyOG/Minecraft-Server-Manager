using MinecraftServerManager.Models;

namespace MinecraftServerManager.ViewModels;

public sealed class SidebarRow
{
    public bool IsHeader { get; init; }

    public string? Header { get; init; }

    public ServerInstance? Instance { get; init; }

    public static SidebarRow Section(string title) => new()
    {
        IsHeader = true,
        Header = title,
        Instance = null
    };

    public static SidebarRow Server(ServerInstance instance) => new()
    {
        IsHeader = false,
        Header = null,
        Instance = instance
    };
}

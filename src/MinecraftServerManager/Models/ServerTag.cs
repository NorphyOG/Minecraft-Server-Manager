namespace MinecraftServerManager.Models;

[Flags]
public enum ServerTag
{
    None = 0,
    Proxy = 1,
    Backend = 2,
    Lobby = 4
}

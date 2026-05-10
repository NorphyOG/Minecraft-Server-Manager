namespace MinecraftServerManager.Models;

public sealed class AppSettings
{
    public string? PreferredJavaPath { get; set; }

    public string ArtifactCacheDirectory { get; set; } = "";

    public bool UseDarkTheme { get; set; } = true;

    public string DefaultJvmArguments { get; set; } = "-Xms512M -Xmx2G";

    public string ServersRootDirectory { get; set; } = "";
}

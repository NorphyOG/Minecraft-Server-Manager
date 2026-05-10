namespace MinecraftServerManager.Services;

public static class PathsHelper
{
    public static string AppDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MinecraftServerManager");

    public static string StateFilePath => Path.Combine(AppDataDirectory, "state.json");

    public static string DefaultServersRoot()
    {
        var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(docs, "MinecraftServerManager", "servers");
    }

    public static string DefaultArtifactCache()
    {
        return Path.Combine(AppDataDirectory, "artifacts");
    }

    public static void EnsureParent(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}

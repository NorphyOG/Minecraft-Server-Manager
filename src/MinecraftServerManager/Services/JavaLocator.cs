using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

public static class JavaLocator
{
    public static string ResolveJavaExecutable(AppSettings settings, ServerInstance? instance)
    {
        if (!string.IsNullOrWhiteSpace(instance?.JavaExecutablePath) && File.Exists(instance.JavaExecutablePath))
            return instance.JavaExecutablePath;

        if (!string.IsNullOrWhiteSpace(settings.PreferredJavaPath) && File.Exists(settings.PreferredJavaPath))
            return settings.PreferredJavaPath;

        return "java";
    }
}

using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

/// <summary>Pfade und Kurzdoku zu <c>plugins/</c>, <c>mods/</c> sowie Loader-/API-Hinweisen.</summary>
public static class ServerExtensionsInfo
{
    public static string PluginsPath(string serverDirectory) =>
        Path.Combine(serverDirectory, "plugins");

    public static string ModsPath(string serverDirectory) =>
        Path.Combine(serverDirectory, "mods");

    public static bool IsModdedLoader(ServerLoaderKind loader) =>
        loader is ServerLoaderKind.Fabric or ServerLoaderKind.Forge;

    public static bool IsBukkitFamily(ServerLoaderKind loader) =>
        loader is ServerLoaderKind.Paper or ServerLoaderKind.Purpur;

    /// <summary>Kurze Warnung bei offensichtlichem Stack-Konflikt (ohne Dateiscan).</summary>
    public static string? LoaderMismatchHint(ServerInstance instance)
    {
        return instance.Loader switch
        {
            ServerLoaderKind.Fabric or ServerLoaderKind.Forge =>
                "Mod-Server: Mods gehören nach mods/. Der Ordner plugins/ ist für Bukkit/Paper-Plugins — auf Forge/Fabric-Servern meist ungeeignet oder nur mit Speziallösungen.",
            ServerLoaderKind.Paper or ServerLoaderKind.Purpur =>
                "Paper/Purpur: Spielinhalt-Plugins liegen in plugins/ (Bukkit-API). Der Ordner mods/ wird vom Vanilla/Paper-Server nicht wie bei Fabric/Forge geladen — Hybrid-Server (z. B. Mohist) nur bewusst einsetzen.",
            ServerLoaderKind.Velocity =>
                "Velocity-Proxy: Nur Velocity-Plugins (und vergleichbare Proxy-Erweiterungen) in plugins/ am Proxy. Spigot-/Paper-Plugins gehören auf die Backend-Server, nicht hier.",
            ServerLoaderKind.Vanilla =>
                "Vanilla-Server unterstützt keine Bukkit-Plugins. Ordner plugins/ ist ohne Modifikation wirkungslos.",
            _ => null
        };
    }

    /// <summary>Ausführliche Hinweise inkl. Cluster-Rolle.</summary>
    public static string BuildGuidance(ServerInstance instance, ClusterDefinition? cluster)
    {
        var proxyBackend = "";
        if (cluster != null)
        {
            var isProxy = cluster.ProxyInstanceId == instance.Id;
            proxyBackend = isProxy
                ? "Diese Instanz ist der Proxy: „plugins/“ hier nur für Velocity-/Proxy-Plugins (z. B. Weiterleitung, Tablist am Proxy). Spiel-Plugins (LuckPerms ingame, Welt-Logik) installieren Sie auf den Backends (Paper), nicht am Proxy."
                : "Diese Instanz ist ein Backend im Cluster: Bukkit/Paper-Plugins, die die Spielwelt betreffen, liegen in plugins/ hier. Proxy-spezifische Plugins gehören nur auf die Velocity-Instanz.";
        }
        else
        {
            proxyBackend =
                "Ohne Cluster: Alle Paper-/Spigot-Plugins für diese Welt liegen in plugins/ unter diesem Serverordner.";
        }

        var apiLine = instance.Loader switch
        {
            ServerLoaderKind.Paper or ServerLoaderKind.Purpur =>
                $"Plugin-Kompatibilität: Achten Sie auf die Minecraft-Version ({instance.MinecraftVersion}) und die angegebene API (Paper/Spigot) auf der Plugin-Seite — Mismatch führt zu Fehlern beim Start.",
            ServerLoaderKind.Velocity =>
                "Proxy-Plugins nutzen die Velocity-API; sie sind **nicht** mit Spigot-Paper-Plugins austauschbar.",
            ServerLoaderKind.Fabric or ServerLoaderKind.Forge =>
                $"Mods müssen zu **Loader und MC-Version ({instance.MinecraftVersion})** passen; Abhängigkeiten (Fabric API, etc.) beachten.",
            _ => "Prüfen Sie die Dokumentation Ihres Servertyps für unterstützte Erweiterungen."
        };

        return string.Join(Environment.NewLine + Environment.NewLine,
            proxyBackend.Trim(),
            apiLine.Trim());
    }
}

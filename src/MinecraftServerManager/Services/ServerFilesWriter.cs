using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

public static class ServerFilesWriter
{
    public static void WriteServerProperties(ServerInstance instance)
    {
        var path = Path.Combine(instance.DirectoryPath, "server.properties");
        Directory.CreateDirectory(instance.DirectoryPath);

        var dict = File.Exists(path)
            ? ReadJavaProps(path)
            : new Dictionary<string, string>();

        dict["server-port"] = instance.ServerPort.ToString();
        dict["level-name"] = instance.LevelName;
        dict["motd"] = dict.GetValueOrDefault("motd", $"§a{instance.Name}");
        dict["online-mode"] = instance.Tags.HasFlag(ServerTag.Backend) ? "false" : "true";

        if (instance.RconEnabled)
        {
            dict["enable-rcon"] = "true";
            dict["rcon.port"] = instance.RconPort.ToString();
            dict["rcon.password"] = instance.RconPassword;
        }

        WriteJavaProps(path, dict);
    }

    public static void WriteEula(ServerInstance instance)
    {
        var path = Path.Combine(instance.DirectoryPath, "eula.txt");
        File.WriteAllText(path,
            "# https://aka.ms/MinecraftEULA\n" +
            (instance.EulaAccepted ? "eula=true\n" : "eula=false\n"));
    }

    public static void EnsurePaperVelocityForwarding(string serverDirectory, string forwardingSecret)
    {
        var cfgDir = Path.Combine(serverDirectory, "config");
        Directory.CreateDirectory(cfgDir);
        var path = Path.Combine(cfgDir, "paper-global.yml");

        var block =
            $"""
            # --- Minecraft Server Manager: Velocity forwarding ---
            proxies:
              velocity:
                enabled: true
                online-mode: false
                secret: '{forwardingSecret.Replace("'", "''")}'
            """;

        if (!File.Exists(path))
        {
            File.WriteAllText(path, block + "\n");
            return;
        }

        var text = File.ReadAllText(path);
        if (text.Contains("velocity:", StringComparison.OrdinalIgnoreCase))
            return;

        File.AppendAllText(path, "\n" + block + "\n");
    }

    private static Dictionary<string, string> ReadJavaProps(string path)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            var t = line.Trim();
            if (string.IsNullOrEmpty(t) || t.StartsWith('#'))
                continue;
            var idx = t.IndexOf('=');
            if (idx <= 0)
                continue;
            dict[t[..idx].Trim()] = t[(idx + 1)..].Trim();
        }

        return dict;
    }

    private static void WriteJavaProps(string path, Dictionary<string, string> dict)
    {
        using var sw = new StreamWriter(path);
        foreach (var kv in dict.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            sw.WriteLine($"{kv.Key}={kv.Value}");
    }
}

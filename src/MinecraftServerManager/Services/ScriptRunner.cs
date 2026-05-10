using System.Diagnostics;
using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

public static class ScriptRunner
{
    public static async Task<(int ExitCode, string Output)> RunHookAsync(
        string scriptPath,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            return (0, "");

        var psi = new ProcessStartInfo
        {
            FileName = scriptPath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase)
                ? "powershell.exe"
                : "cmd.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (psi.FileName.Contains("powershell", StringComparison.OrdinalIgnoreCase))
        {
            psi.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"";
        }
        else
        {
            psi.Arguments = $"/c \"\"{scriptPath}\"\"";
        }

        foreach (var kv in environment)
            psi.Environment[kv.Key] = kv.Value;

        using var proc = Process.Start(psi);
        if (proc == null)
            return (-1, "Prozess konnte nicht gestartet werden.");

        var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        var combined = stdout + stderr;
        return (proc.ExitCode, combined.Trim());
    }

    public static IReadOnlyDictionary<string, string> BuildEnv(ServerInstance instance)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MSM_INSTANCE_DIR"] = instance.DirectoryPath,
            ["MSM_INSTANCE_ID"] = instance.Id.ToString(),
            ["MSM_INSTANCE_NAME"] = instance.Name,
            ["MSM_PORT"] = instance.ServerPort.ToString(),
            ["MSM_LOADER"] = instance.Loader.ToString(),
            ["MSM_MC_VERSION"] = instance.MinecraftVersion
        };
    }
}

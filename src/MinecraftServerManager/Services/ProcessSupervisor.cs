using System.Collections.Concurrent;
using System.Diagnostics;
using MinecraftServerManager.Models;

namespace MinecraftServerManager.Services;

public sealed class ProcessSupervisor : IDisposable
{
    private sealed class RunningEntry
    {
        public required Process Process { get; init; }
        public required CancellationTokenSource ReaderCts { get; init; }
    }

    private readonly ConcurrentDictionary<Guid, RunningEntry> _running = new();

    /// <summary>Fires when the Minecraft/Velocity process exits unexpectedly or after stop.</summary>
    public event Action<Guid>? ProcessExited;

    public bool IsRunning(Guid instanceId) => _running.ContainsKey(instanceId);

    public async Task<(bool Ok, string? Error)> StartAsync(
        ServerInstance instance,
        string javaExecutable,
        Action<string> onStdout,
        Action<string> onStderr,
        CancellationToken cancellationToken)
    {
        if (_running.ContainsKey(instance.Id))
            return (false, "Server läuft bereits.");

        var jarPath = Path.Combine(instance.DirectoryPath, instance.JarFileName);
        if (!File.Exists(jarPath))
            return (false, $"Datei nicht gefunden: {jarPath}");

        var nogui = instance.AppendNogui ? " nogui" : "";
        var args = $"{instance.JvmArguments.Trim()} -jar \"{instance.JarFileName}\"{nogui}";

        var psi = new ProcessStartInfo
        {
            FileName = javaExecutable,
            Arguments = args,
            WorkingDirectory = instance.DirectoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
        };

        try
        {
            var proc = new Process { StartInfo = psi };
            proc.EnableRaisingEvents = true;
            var readerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (!proc.Start())
                return (false, "Prozess konnte nicht gestartet werden.");

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!readerCts.Token.IsCancellationRequested && proc.StandardOutput.ReadLine() is { } line)
                        onStdout(line);
                }
                catch { /* ignored */ }
            }, readerCts.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!readerCts.Token.IsCancellationRequested && proc.StandardError.ReadLine() is { } line)
                        onStderr(line);
                }
                catch { /* ignored */ }
            }, readerCts.Token);

            proc.Exited += (_, _) =>
            {
                readerCts.Cancel();
                _running.TryRemove(instance.Id, out _);
                ProcessExited?.Invoke(instance.Id);
            };

            _running[instance.Id] = new RunningEntry { Process = proc, ReaderCts = readerCts };
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public bool SendCommand(Guid instanceId, string line)
    {
        if (!_running.TryGetValue(instanceId, out var entry))
            return false;
        try
        {
            entry.Process.StandardInput.WriteLine(line);
            entry.Process.StandardInput.Flush();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task StopGracefullyAsync(Guid instanceId, TimeSpan waitForExit, CancellationToken ct)
    {
        if (!_running.TryGetValue(instanceId, out var entry))
            return;

        try
        {
            entry.Process.StandardInput.WriteLine("stop");
            entry.Process.StandardInput.Flush();
        }
        catch { /* ignored */ }

        var completed = await Task.Run(() => entry.Process.WaitForExit((int)waitForExit.TotalMilliseconds), ct);
        if (!completed && !entry.Process.HasExited)
        {
            try
            {
                entry.Process.Kill(entireProcessTree: true);
            }
            catch { /* ignored */ }
        }

        entry.ReaderCts.Cancel();
        _running.TryRemove(instanceId, out _);
        try
        {
            entry.Process.Dispose();
        }
        catch { /* ignored */ }
    }

    public async Task StopAllAsync(TimeSpan wait, CancellationToken ct)
    {
        foreach (var id in _running.Keys.ToArray())
            await StopGracefullyAsync(id, wait, ct);
    }

    public void Dispose()
    {
        foreach (var kv in _running)
        {
            try
            {
                kv.Value.ReaderCts.Cancel();
                if (!kv.Value.Process.HasExited)
                    kv.Value.Process.Kill(entireProcessTree: true);
                kv.Value.Process.Dispose();
            }
            catch { /* ignored */ }
        }
        _running.Clear();
    }
}

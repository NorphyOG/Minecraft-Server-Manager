using System.Net.NetworkInformation;

namespace MinecraftServerManager.Services;

public static class PortChecker
{
    public static bool IsTcpPortInUse(int port)
    {
        var props = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = props.GetActiveTcpListeners();
        return listeners.Any(e => e.Port == port);
    }

    public static int FindFreePort(int start, int maxAttempts = 64)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            var p = start + i;
            if (p > 65535)
                break;
            if (!IsTcpPortInUse(p))
                return p;
        }

        throw new InvalidOperationException("Kein freier Port gefunden.");
    }
}

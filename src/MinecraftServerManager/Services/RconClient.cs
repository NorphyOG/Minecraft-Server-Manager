using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace MinecraftServerManager.Services;

/// <summary>Minecraft RCON (minimal single-response).</summary>
public static class RconClient
{
    public static async Task<(bool Ok, string Response)> SendAsync(
        string host,
        int port,
        string password,
        string command,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var tcp = new TcpClient();
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(timeout);
        await tcp.ConnectAsync(host, port, connectCts.Token);

        await using var stream = tcp.GetStream();

        var authId = Random.Shared.Next(1, int.MaxValue - 1);
        await SendPacketAsync(stream, 3, authId, password, connectCts.Token);
        var auth = await ReadPacketAsync(stream, connectCts.Token);
        if (auth.RequestId == -1)
            return (false, "RCON Authentifizierung fehlgeschlagen.");

        var cmdId = authId + 1;
        await SendPacketAsync(stream, 2, cmdId, command, connectCts.Token);
        var cmd = await ReadPacketAsync(stream, connectCts.Token);
        return (true, cmd.Body.TrimEnd('\0'));
    }

    private static async Task SendPacketAsync(NetworkStream stream, int type, int reqId, string body,
        CancellationToken ct)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body + "\0");
        var len = 4 + 4 + bodyBytes.Length;
        var packet = new byte[4 + len];
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(0, 4), len);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(4, 4), reqId);
        BinaryPrimitives.WriteInt32LittleEndian(packet.AsSpan(8, 4), type);
        bodyBytes.CopyTo(packet.AsSpan(12));
        await stream.WriteAsync(packet.AsMemory(0, 12 + bodyBytes.Length), ct);
    }

    private readonly record struct Packet(int RequestId, string Body);

    private static async Task<Packet> ReadPacketAsync(NetworkStream stream, CancellationToken ct)
    {
        var lenBuf = new byte[4];
        await ReadExactAsync(stream, lenBuf, ct);
        var len = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);
        if (len < 10 || len > 4096 * 1024)
            throw new InvalidDataException("Ungültige RCON-Antwort.");

        var body = new byte[len];
        await ReadExactAsync(stream, body, ct);
        var reqId = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(0, 4));
        var bodyTextEnd = len - 2;
        var textLen = Math.Max(0, bodyTextEnd - 8);
        var bodyText = Encoding.UTF8.GetString(body.AsSpan(8, textLen));
        return new Packet(reqId, bodyText);
    }

    private static async Task ReadExactAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken ct)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer[read..], ct);
            if (n == 0)
                throw new EndOfStreamException();
            read += n;
        }
    }
}

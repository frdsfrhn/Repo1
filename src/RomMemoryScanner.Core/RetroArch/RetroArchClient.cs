using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RomMemoryScanner.Core.RetroArch;

/// <summary>
/// Talks to RetroArch's UDP Network Command Interface (READ_CORE_RAM / WRITE_CORE_RAM / VERSION /
/// GET_STATUS), per FR-6.1 and §5.2. This is the preferred, sanctioned integration path — it
/// avoids raw process-memory access (and the AV friction that comes with it) entirely, which is
/// why it is the only Mode-A transport implemented in Phase 1 (§9).
///
/// RetroArch must have "Network Commands" enabled (Settings > Network) and typically listens on
/// UDP port 55355 on localhost.
/// </summary>
public sealed class RetroArchClient : IDisposable
{
    public const int DefaultPort = 55355;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(750);

    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _endpoint;

    public RetroArchClient(string host = "127.0.0.1", int port = DefaultPort)
    {
        _endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        _udpClient = new UdpClient();
        _udpClient.Client.ReceiveTimeout = (int)DefaultTimeout.TotalMilliseconds;
    }

    /// <summary>Sends VERSION and waits for a reply, to confirm RetroArch is running and reachable.</summary>
    public async Task<string?> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        string? reply = await SendCommandAsync("VERSION", cancellationToken).ConfigureAwait(false);
        return reply?.Trim();
    }

    public async Task<string?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        string? reply = await SendCommandAsync("GET_STATUS", cancellationToken).ConfigureAwait(false);
        return reply?.Trim();
    }

    /// <summary>
    /// Reads <paramref name="length"/> bytes starting at the given core-relative RAM offset
    /// (see <see cref="Addressing.AddressTranslator"/> — this is NOT a console-space address).
    /// </summary>
    public async Task<byte[]> ReadCoreRamAsync(uint coreOffset, int length, CancellationToken cancellationToken = default)
    {
        string command = $"READ_CORE_RAM {coreOffset:x} {length}";
        string? reply = await SendCommandAsync(command, cancellationToken).ConfigureAwait(false)
            ?? throw new RetroArchCommunicationException("No response from RetroArch to READ_CORE_RAM (is it running with network commands enabled?).");

        return ParseReadCoreRamReply(reply, coreOffset, length);
    }

    /// <summary>Writes <paramref name="data"/> starting at the given core-relative RAM offset.</summary>
    public async Task WriteCoreRamAsync(uint coreOffset, byte[] data, CancellationToken cancellationToken = default)
    {
        string hexBytes = string.Join(' ', data.Select(b => b.ToString("x2")));
        string command = $"WRITE_CORE_RAM {coreOffset:x} {hexBytes}";
        await SendCommandAsync(command, cancellationToken).ConfigureAwait(false);
        // RetroArch does not reliably reply to WRITE_CORE_RAM with a parseable success payload
        // across versions, so absence of a reply is not treated as failure here; callers that
        // need write confirmation should follow up with a read (as the dashboard does).
    }

    internal static byte[] ParseReadCoreRamReply(string reply, uint expectedOffset, int expectedLength)
    {
        // Expected format: "READ_CORE_RAM <offset_hex> <byte> <byte> ..." e.g. "READ_CORE_RAM 7e0 8b 04"
        // On failure RetroArch replies "READ_CORE_RAM <offset_hex> -1".
        string[] parts = reply.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !string.Equals(parts[0], "READ_CORE_RAM", StringComparison.OrdinalIgnoreCase))
        {
            throw new RetroArchCommunicationException($"Unexpected reply to READ_CORE_RAM: '{reply}'.");
        }

        if (parts.Length == 3 && parts[2] == "-1")
        {
            throw new RetroArchCommunicationException(
                $"RetroArch rejected READ_CORE_RAM at offset 0x{expectedOffset:x} — no core loaded, or offset outside the core's RAM.");
        }

        byte[] bytes = parts.Skip(2).Select(hex => Convert.ToByte(hex, 16)).ToArray();
        if (bytes.Length != expectedLength)
        {
            throw new RetroArchCommunicationException(
                $"Expected {expectedLength} bytes from READ_CORE_RAM, got {bytes.Length}.");
        }

        return bytes;
    }

    private async Task<string?> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        byte[] payload = Encoding.ASCII.GetBytes(command);
        await _udpClient.SendAsync(payload, payload.Length, _endpoint).ConfigureAwait(false);

        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        try
        {
            UdpReceiveResult result = await _udpClient.ReceiveAsync(linkedCts.Token).ConfigureAwait(false);
            return Encoding.ASCII.GetString(result.Buffer);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public void Dispose() => _udpClient.Dispose();
}

public sealed class RetroArchCommunicationException : Exception
{
    public RetroArchCommunicationException(string message) : base(message)
    {
    }
}

using System.Net;
using System.Net.Sockets;
using System.Text;
using RomMemoryScanner.Core.RetroArch;

namespace RomMemoryScanner.Tests.RetroArch;

/// <summary>
/// A minimal local UDP stand-in for RetroArch's Network Command Interface, used to test
/// <see cref="RetroArchClient"/>'s real socket behavior under concurrency — the kind of bug this
/// exists to catch (requests/replies from different callers interleaving on one socket) can't be
/// reproduced against a fake in-memory transport; it only shows up with real UDP semantics.
/// Replies to READ_CORE_RAM with deterministic bytes derived from the requested offset, after a
/// randomized delay, so that any cross-talk between concurrent requests would surface as
/// mismatched data instead of the request that was actually sent.
/// </summary>
public sealed class FakeRetroArchServer : IDisposable
{
    private readonly UdpClient _server;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;

    public FakeRetroArchServer()
    {
        _server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        Port = ((IPEndPoint)_server.Client.LocalEndPoint!).Port;
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public int Port { get; }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await _server.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            // Handle each request on its own task so a slow one doesn't block receiving the next —
            // this is what makes overlapping requests possible to reproduce in the test.
            _ = Task.Run(() => HandleRequestAsync(result, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleRequestAsync(UdpReceiveResult result, CancellationToken cancellationToken)
    {
        string command = Encoding.ASCII.GetString(result.Buffer);
        string[] parts = command.Split(' ');

        // Randomized delay to encourage requests overlapping in flight, which is exactly the
        // condition that exposed the cross-talk bug against a real RetroArch instance.
        await Task.Delay(Random.Shared.Next(1, 15), cancellationToken).ConfigureAwait(false);

        if (parts.Length >= 2 && parts[0] == "WRITE_CORE_RAM")
        {
            byte[] ack = Encoding.ASCII.GetBytes($"WRITE_CORE_RAM {parts[1]}");
            await _server.SendAsync(ack, ack.Length, result.RemoteEndPoint).ConfigureAwait(false);
            return;
        }

        if (parts.Length != 3 || parts[0] != "READ_CORE_RAM")
        {
            return;
        }

        uint offset = Convert.ToUInt32(parts[1], 16);
        int length = int.Parse(parts[2]);

        var reply = new StringBuilder($"READ_CORE_RAM {parts[1]}");
        for (int i = 0; i < length; i++)
        {
            byte value = (byte)((offset + i) & 0xFF);
            reply.Append(' ').Append(value.ToString("x2"));
        }

        byte[] payload = Encoding.ASCII.GetBytes(reply.ToString());
        await _server.SendAsync(payload, payload.Length, result.RemoteEndPoint).ConfigureAwait(false);
    }

    /// <summary>The deterministic value <see cref="RunAsync"/> would reply with for a given offset — for test assertions.</summary>
    public static byte ExpectedByte(uint offset) => (byte)(offset & 0xFF);

    public void Dispose()
    {
        _cts.Cancel();
        _server.Dispose();
        _cts.Dispose();
    }
}

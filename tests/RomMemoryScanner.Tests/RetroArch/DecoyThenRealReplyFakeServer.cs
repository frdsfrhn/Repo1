using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RomMemoryScanner.Tests.RetroArch;

/// <summary>
/// For the first request it receives, sends a few decoy replies (for a made-up, unrelated offset)
/// immediately before the real one — deterministically reproducing "a stale reply is already
/// sitting in the receive buffer when this request's own reply arrives," without depending on
/// real-world timing races the way <see cref="FakeRetroArchServer"/>'s delayed-first-reply mode
/// does. Precisely targets whether <c>RetroArchClient.SendCommandAsync</c>'s discard loop actually
/// skips a mismatched reply and keeps waiting, rather than accepting the first thing that arrives.
/// </summary>
public sealed class DecoyThenRealReplyFakeServer : IDisposable
{
    private readonly UdpClient _server;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private readonly int _decoyCount;
    private bool _handledFirst;

    public DecoyThenRealReplyFakeServer(int decoyCount = 4)
    {
        _decoyCount = decoyCount;
        _server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        Port = ((IPEndPoint)_server.Client.LocalEndPoint!).Port;
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public int Port { get; }

    public static byte ExpectedByte(uint offset) => (byte)(offset & 0xFF);

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

            string command = Encoding.ASCII.GetString(result.Buffer);
            string[] parts = command.Split(' ');
            if (parts.Length != 3 || parts[0] != "READ_CORE_RAM")
            {
                continue;
            }

            uint offset = Convert.ToUInt32(parts[1], 16);
            int length = int.Parse(parts[2]);

            if (!_handledFirst)
            {
                _handledFirst = true;

                // Decoys: valid-looking READ_CORE_RAM replies, but for a different offset than
                // what was actually requested — exactly what a late reply to an earlier,
                // already-abandoned request looks like from the receiver's side.
                for (int i = 0; i < _decoyCount; i++)
                {
                    uint decoyOffset = 0xDEC000 + (uint)i;
                    byte[] decoy = Encoding.ASCII.GetBytes($"READ_CORE_RAM {decoyOffset:x} {ExpectedByte(decoyOffset):x2}");
                    await _server.SendAsync(decoy, decoy.Length, result.RemoteEndPoint).ConfigureAwait(false);
                }
            }

            var reply = new StringBuilder($"READ_CORE_RAM {parts[1]}");
            for (int i = 0; i < length; i++)
            {
                reply.Append(' ').Append(ExpectedByte(offset + (uint)i).ToString("x2"));
            }

            byte[] payload = Encoding.ASCII.GetBytes(reply.ToString());
            await _server.SendAsync(payload, payload.Length, result.RemoteEndPoint).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _server.Dispose();
        _cts.Dispose();
    }
}

using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.Scanning;

namespace RomMemoryScanner.Core.RetroArch;

/// <summary>
/// Reads a console's entire WRAM window from RetroArch via chunked READ_CORE_RAM calls (NFR-3:
/// a scan pass should complete in well under a second on typical hardware — chunking keeps each
/// individual UDP round-trip small while keeping the total request count reasonable).
/// </summary>
public sealed class RetroArchSnapshotReader : IMemorySnapshotReader
{
    private const int ChunkSize = 8192;

    private readonly RetroArchClient _client;
    private readonly ConsoleAddressSpace _space;

    public RetroArchSnapshotReader(RetroArchClient client, ConsoleType console)
    {
        _client = client;
        _space = ConsoleAddressSpace.For(console);
    }

    public async Task<byte[]> ReadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new byte[_space.WramSize];
        uint offset = 0;
        while (offset < _space.WramSize)
        {
            int length = (int)Math.Min(ChunkSize, _space.WramSize - offset);
            byte[] chunk = await _client.ReadCoreRamAsync(offset, length, cancellationToken).ConfigureAwait(false);
            Array.Copy(chunk, 0, buffer, offset, length);
            offset += (uint)length;
        }

        return buffer;
    }
}

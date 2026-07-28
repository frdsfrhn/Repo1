using RomMemoryScanner.Core.Scanning;

namespace RomMemoryScanner.Tests.Scanning;

/// <summary>
/// Stands in for a real RetroArch connection in tests: the test mutates <see cref="CurrentSnapshot"/>
/// between scan passes to simulate the game's own writes (e.g. HP dropping after taking damage).
/// </summary>
public sealed class FakeMemorySnapshotReader : IMemorySnapshotReader
{
    public FakeMemorySnapshotReader(byte[] initialSnapshot)
    {
        CurrentSnapshot = initialSnapshot;
    }

    public byte[] CurrentSnapshot { get; set; }

    public int CallCount { get; private set; }

    public Task<byte[]> ReadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult((byte[])CurrentSnapshot.Clone());
    }
}

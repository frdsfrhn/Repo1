namespace RomMemoryScanner.Core.Scanning;

/// <summary>
/// Reads a full snapshot of a console's WRAM window. Abstracted away from
/// <see cref="MemoryScanner"/> so the narrowing logic (the part that actually matters to get
/// right) can be unit-tested without a real RetroArch connection — see
/// <see cref="RetroArch.RetroArchSnapshotReader"/> for the real implementation.
/// </summary>
public interface IMemorySnapshotReader
{
    /// <summary>Returns a buffer of exactly the console's WRAM size, byte-for-byte, offset 0 = WRAM base.</summary>
    Task<byte[]> ReadSnapshotAsync(CancellationToken cancellationToken = default);
}

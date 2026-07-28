using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.Numeric;

namespace RomMemoryScanner.Core.Scanning;

/// <summary>
/// Cheat Engine-style value-scan workflow (FR-2.4): an initial full-WRAM scan for a known value,
/// then repeated narrowing passes ("increased"/"decreased"/"changed"/"unchanged", or a new known
/// value) until a small candidate list remains. Each pass re-reads the whole WRAM window once and
/// does all comparison work in memory — see <see cref="RetroArch.RetroArchSnapshotReader"/> for
/// why that's cheaper than one network round-trip per candidate.
///
/// Values here are raw (pre-BCD-decode) — callers displaying/accepting BCD or signed values
/// should convert at the UI boundary, the same way the dashboard does (see
/// <see cref="Numeric.BcdConverter"/>).
/// </summary>
public sealed class MemoryScanner
{
    private readonly IMemorySnapshotReader _reader;
    private readonly ConsoleAddressSpace _space;

    private byte[]? _previousSnapshot;
    private List<uint>? _candidateOffsets;
    private DataType _dataType;
    private ByteOrder _byteOrder;

    public MemoryScanner(IMemorySnapshotReader reader, ConsoleType console)
    {
        _reader = reader;
        _space = ConsoleAddressSpace.For(console);
    }

    public bool HasActiveScan => _candidateOffsets is not null;

    public int CandidateCount => _candidateOffsets?.Count ?? 0;

    /// <summary>FR-2.4 initial scan: finds every offset in WRAM currently holding <paramref name="knownRawValue"/>.</summary>
    public async Task<int> InitialScanAsync(uint knownRawValue, DataType dataType, ByteOrder byteOrder, CancellationToken cancellationToken = default)
    {
        _dataType = dataType;
        _byteOrder = byteOrder;

        byte[] snapshot = await _reader.ReadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        int byteWidth = dataType.ByteWidth();

        var candidates = new List<uint>();
        for (int offset = 0; offset + byteWidth <= snapshot.Length; offset++)
        {
            uint value = RawValueCodec.FromBytes(snapshot.AsSpan(offset, byteWidth), byteOrder);
            if (value == knownRawValue)
            {
                candidates.Add((uint)offset);
            }
        }

        _previousSnapshot = snapshot;
        _candidateOffsets = candidates;
        return candidates.Count;
    }

    /// <summary>FR-2.4 next scan: narrows the current candidates by how their value changed since the last pass.</summary>
    public async Task<int> NextScanAsync(ScanComparison comparison, uint? exactRawValue = null, CancellationToken cancellationToken = default)
    {
        if (_candidateOffsets is null || _previousSnapshot is null)
        {
            throw new InvalidOperationException($"Call {nameof(InitialScanAsync)} before {nameof(NextScanAsync)}.");
        }

        if (comparison == ScanComparison.EqualTo && exactRawValue is null)
        {
            throw new ArgumentException($"{nameof(exactRawValue)} is required when {nameof(comparison)} is {nameof(ScanComparison.EqualTo)}.", nameof(exactRawValue));
        }

        byte[] snapshot = await _reader.ReadSnapshotAsync(cancellationToken).ConfigureAwait(false);
        int byteWidth = _dataType.ByteWidth();

        var survivors = new List<uint>(_candidateOffsets.Count);
        foreach (uint offset in _candidateOffsets)
        {
            uint oldValue = RawValueCodec.FromBytes(_previousSnapshot.AsSpan((int)offset, byteWidth), _byteOrder);
            uint newValue = RawValueCodec.FromBytes(snapshot.AsSpan((int)offset, byteWidth), _byteOrder);

            bool survives = comparison switch
            {
                ScanComparison.EqualTo => newValue == exactRawValue!.Value,
                ScanComparison.Increased => newValue > oldValue,
                ScanComparison.Decreased => newValue < oldValue,
                ScanComparison.Changed => newValue != oldValue,
                ScanComparison.Unchanged => newValue == oldValue,
                _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null),
            };

            if (survives)
            {
                survivors.Add(offset);
            }
        }

        _previousSnapshot = snapshot;
        _candidateOffsets = survivors;
        return survivors.Count;
    }

    /// <summary>Returns up to <paramref name="limit"/> current candidates (console-space address + current value); use <see cref="CandidateCount"/> for the true total.</summary>
    public IReadOnlyList<ScanCandidate> GetCandidates(int limit = 200)
    {
        if (_candidateOffsets is null || _previousSnapshot is null)
        {
            return Array.Empty<ScanCandidate>();
        }

        int byteWidth = _dataType.ByteWidth();
        var result = new List<ScanCandidate>(Math.Min(limit, _candidateOffsets.Count));
        foreach (uint offset in _candidateOffsets.Take(limit))
        {
            uint value = RawValueCodec.FromBytes(_previousSnapshot.AsSpan((int)offset, byteWidth), _byteOrder);
            result.Add(new ScanCandidate(_space.WramBase + offset, value));
        }

        return result;
    }

    public void Reset()
    {
        _previousSnapshot = null;
        _candidateOffsets = null;
    }
}

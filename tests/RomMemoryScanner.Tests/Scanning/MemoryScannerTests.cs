using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.Scanning;
using Xunit;

namespace RomMemoryScanner.Tests.Scanning;

/// <summary>
/// Simulates the FR-2.4 guided-search workflow end to end: place a known "HP" value at a real
/// offset plus decoys elsewhere in a fake SNES WRAM buffer, then narrow via each comparison
/// operator exactly the way a player would after taking damage / healing / doing nothing.
/// </summary>
public class MemoryScannerTests
{
    private static byte[] NewSnesWram() => new byte[ConsoleAddressSpace.Snes.WramSize];

    [Fact]
    public async Task InitialScan_FindsAllOffsetsHoldingTheKnownValue()
    {
        byte[] wram = NewSnesWram();
        wram[0x100] = 87; // real HP
        wram[0x200] = 87; // decoy, will stay unchanged
        wram[0x300] = 87; // decoy, will increase

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);

        int count = await scanner.InitialScanAsync(87, DataType.U8, ByteOrder.LittleEndian);

        Assert.Equal(3, count);
        Assert.Equal(3, scanner.CandidateCount);
        Assert.True(scanner.HasActiveScan);
    }

    [Fact]
    public async Task NextScan_Decreased_NarrowsToOnlyTheAddressThatDropped()
    {
        byte[] wram = NewSnesWram();
        wram[0x100] = 87;
        wram[0x200] = 87;
        wram[0x300] = 87;

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(87, DataType.U8, ByteOrder.LittleEndian);

        // Simulate taking damage (real HP drops), a decoy staying put, and a decoy increasing.
        var next = (byte[])wram.Clone();
        next[0x100] = 80;
        next[0x200] = 87;
        next[0x300] = 90;
        reader.CurrentSnapshot = next;

        int count = await scanner.NextScanAsync(ScanComparison.Decreased);

        Assert.Equal(1, count);
        ScanCandidate candidate = Assert.Single(scanner.GetCandidates());
        Assert.Equal(ConsoleAddressSpace.Snes.WramBase + 0x100, candidate.ConsoleAddress);
        Assert.Equal(80u, candidate.Value);
    }

    [Fact]
    public async Task NextScan_Unchanged_KeepsOnlyAddressesThatDidNotMove()
    {
        byte[] wram = NewSnesWram();
        wram[0x100] = 87;
        wram[0x200] = 87;

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(87, DataType.U8, ByteOrder.LittleEndian);

        var next = (byte[])wram.Clone();
        next[0x100] = 80; // changed
        next[0x200] = 87; // unchanged
        reader.CurrentSnapshot = next;

        int count = await scanner.NextScanAsync(ScanComparison.Unchanged);

        Assert.Equal(1, count);
        Assert.Equal(ConsoleAddressSpace.Snes.WramBase + 0x200, scanner.GetCandidates()[0].ConsoleAddress);
    }

    [Fact]
    public async Task NextScan_Changed_KeepsOnlyAddressesThatMoved()
    {
        byte[] wram = NewSnesWram();
        wram[0x100] = 87;
        wram[0x200] = 87;

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(87, DataType.U8, ByteOrder.LittleEndian);

        var next = (byte[])wram.Clone();
        next[0x100] = 80;
        next[0x200] = 87;
        reader.CurrentSnapshot = next;

        int count = await scanner.NextScanAsync(ScanComparison.Changed);

        Assert.Equal(1, count);
        Assert.Equal(ConsoleAddressSpace.Snes.WramBase + 0x100, scanner.GetCandidates()[0].ConsoleAddress);
    }

    [Fact]
    public async Task NextScan_Increased_KeepsOnlyAddressesThatWentUp()
    {
        byte[] wram = NewSnesWram();
        wram[0x100] = 87;
        wram[0x300] = 87;

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(87, DataType.U8, ByteOrder.LittleEndian);

        var next = (byte[])wram.Clone();
        next[0x100] = 80;
        next[0x300] = 90;
        reader.CurrentSnapshot = next;

        int count = await scanner.NextScanAsync(ScanComparison.Increased);

        Assert.Equal(1, count);
        Assert.Equal(ConsoleAddressSpace.Snes.WramBase + 0x300, scanner.GetCandidates()[0].ConsoleAddress);
    }

    [Fact]
    public async Task NextScan_EqualTo_NarrowsToASpecificNewKnownValue()
    {
        byte[] wram = NewSnesWram();
        wram[0x100] = 87;
        wram[0x200] = 87;

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(87, DataType.U8, ByteOrder.LittleEndian);

        var next = (byte[])wram.Clone();
        next[0x100] = 80; // player tells the app "my HP is now 80"
        next[0x200] = 87;
        reader.CurrentSnapshot = next;

        int count = await scanner.NextScanAsync(ScanComparison.EqualTo, exactRawValue: 80);

        Assert.Equal(1, count);
        Assert.Equal(ConsoleAddressSpace.Snes.WramBase + 0x100, scanner.GetCandidates()[0].ConsoleAddress);
    }

    [Fact]
    public async Task MultiPassNarrowing_ConvergesToSingleAddress()
    {
        byte[] wram = NewSnesWram();
        wram[0x100] = 87; // real HP
        wram[0x200] = 87; // decoy: happens to match every step until the last
        wram[0x300] = 87; // decoy: diverges on pass 1

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(87, DataType.U8, ByteOrder.LittleEndian);

        var pass1 = (byte[])wram.Clone();
        pass1[0x100] = 80; pass1[0x200] = 80; pass1[0x300] = 90;
        reader.CurrentSnapshot = pass1;
        await scanner.NextScanAsync(ScanComparison.Decreased); // eliminates 0x300, keeps 0x100 and 0x200

        var pass2 = (byte[])wram.Clone();
        pass2[0x100] = 75; pass2[0x200] = 80; // 0x200 stops moving; 0x100 keeps dropping
        reader.CurrentSnapshot = pass2;
        int finalCount = await scanner.NextScanAsync(ScanComparison.Decreased);

        Assert.Equal(1, finalCount);
        Assert.Equal(ConsoleAddressSpace.Snes.WramBase + 0x100, scanner.GetCandidates()[0].ConsoleAddress);
    }

    [Fact]
    public async Task Scan_RespectsByteWidthAndBigEndianByteOrder_LikeGenesis()
    {
        byte[] wram = new byte[ConsoleAddressSpace.Genesis.WramSize];
        // Genesis is big-endian: value 0x1234 stored as bytes [0x12, 0x34].
        wram[0x50] = 0x12;
        wram[0x51] = 0x34;

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Genesis);

        int count = await scanner.InitialScanAsync(0x1234, DataType.U16, ByteOrder.BigEndian);

        Assert.Equal(1, count);
        Assert.Equal(ConsoleAddressSpace.Genesis.WramBase + 0x50, scanner.GetCandidates()[0].ConsoleAddress);
    }

    [Fact]
    public async Task NextScan_BeforeInitialScan_Throws()
    {
        var reader = new FakeMemorySnapshotReader(NewSnesWram());
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);

        await Assert.ThrowsAsync<InvalidOperationException>(() => scanner.NextScanAsync(ScanComparison.Changed));
    }

    [Fact]
    public async Task NextScan_EqualToWithoutValue_Throws()
    {
        var reader = new FakeMemorySnapshotReader(NewSnesWram());
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(0, DataType.U8, ByteOrder.LittleEndian);

        await Assert.ThrowsAsync<ArgumentException>(() => scanner.NextScanAsync(ScanComparison.EqualTo));
    }

    [Fact]
    public async Task Reset_ClearsScanState()
    {
        var reader = new FakeMemorySnapshotReader(NewSnesWram());
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        await scanner.InitialScanAsync(0, DataType.U8, ByteOrder.LittleEndian);

        scanner.Reset();

        Assert.False(scanner.HasActiveScan);
        Assert.Equal(0, scanner.CandidateCount);
        Assert.Empty(scanner.GetCandidates());
    }

    [Fact]
    public async Task GetCandidates_RespectsLimit_WhileCandidateCountReportsTrueTotal()
    {
        byte[] wram = NewSnesWram(); // all zero -> every offset matches value 0
        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);

        int total = await scanner.InitialScanAsync(0, DataType.U8, ByteOrder.LittleEndian);

        Assert.Equal(wram.Length, total);
        Assert.Equal(wram.Length, scanner.CandidateCount);
        Assert.Equal(10, scanner.GetCandidates(limit: 10).Count);
    }

    [Fact]
    public async Task InitialScanUnknown_StartsWithEveryOffsetAsACandidate()
    {
        byte[] wram = NewSnesWram();
        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);

        int count = await scanner.InitialScanUnknownAsync(DataType.U8, ByteOrder.LittleEndian);

        Assert.Equal(wram.Length, count);
        Assert.True(scanner.HasActiveScan);
    }

    [Fact]
    public async Task InitialScanUnknown_ThenChanged_FindsAnItemSlotStyleAddress_WithNoKnownValue()
    {
        // Simulates finding an item slot: no visible number to type in, just "swap the item and
        // tell me what changed" (the Cheat Engine-style "unknown initial value" search).
        byte[] wram = NewSnesWram();
        wram[0x2EB] = 0x05; // "item slot" holding item id 5 (Herb), unknown to the scanner

        var reader = new FakeMemorySnapshotReader(wram);
        var scanner = new MemoryScanner(reader, ConsoleType.Snes);
        int initialCount = await scanner.InitialScanUnknownAsync(DataType.U8, ByteOrder.LittleEndian);
        Assert.Equal(wram.Length, initialCount); // nothing filtered out yet

        // Player swaps the item in that slot for a Mithril Sword (id 0x14); nothing else changes.
        var afterSwap = (byte[])wram.Clone();
        afterSwap[0x2EB] = 0x14;
        reader.CurrentSnapshot = afterSwap;

        int narrowed = await scanner.NextScanAsync(ScanComparison.Changed);

        Assert.Equal(1, narrowed);
        ScanCandidate candidate = Assert.Single(scanner.GetCandidates());
        Assert.Equal(ConsoleAddressSpace.Snes.WramBase + 0x2EB, candidate.ConsoleAddress);
        Assert.Equal(0x14u, candidate.Value);
    }
}

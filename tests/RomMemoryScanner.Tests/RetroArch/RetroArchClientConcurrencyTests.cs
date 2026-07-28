using RomMemoryScanner.Core.RetroArch;
using Xunit;

namespace RomMemoryScanner.Tests.RetroArch;

/// <summary>
/// Regression test for a real bug found via live testing: the Dashboard polling several rows
/// (each a separate READ_CORE_RAM call) produced "offset mismatch" errors where each row's value
/// came back as its neighbor's — concurrent requests sharing one UDP socket with no
/// synchronization, so replies got picked up by whichever concurrent receive unblocked first.
/// Reproduces the concurrent load pattern (many overlapping reads, same client, same as
/// Dashboard-polls-many-rows or Dashboard-polls-while-Live-Scan-runs) against a real local UDP
/// server and asserts every single one comes back correct.
/// </summary>
public class RetroArchClientConcurrencyTests
{
    [Fact]
    public async Task ConcurrentReads_AllReturnCorrectData_NoCrossTalk()
    {
        using var server = new FakeRetroArchServer();
        using var client = new RetroArchClient("127.0.0.1", server.Port);

        // Distinct offsets so any cross-talk (getting a different offset's reply) is detectable.
        uint[] offsets = Enumerable.Range(0, 40).Select(i => (uint)(i * 0x100)).ToArray();

        Task<byte[]>[] tasks = offsets.Select(offset => client.ReadCoreRamAsync(offset, 4)).ToArray();
        byte[][] results = await Task.WhenAll(tasks);

        for (int i = 0; i < offsets.Length; i++)
        {
            byte expected = FakeRetroArchServer.ExpectedByte(offsets[i]);
            Assert.Equal(expected, results[i][0]);
        }
    }

    [Fact]
    public async Task ConcurrentReads_InterleavedWithWrites_AllReadsReturnCorrectData()
    {
        using var server = new FakeRetroArchServer();
        using var client = new RetroArchClient("127.0.0.1", server.Port);

        uint[] readOffsets = Enumerable.Range(0, 20).Select(i => (uint)(i * 0x40)).ToArray();

        var readTasks = readOffsets.Select(offset => client.ReadCoreRamAsync(offset, 2)).ToArray();
        var writeTasks = Enumerable.Range(0, 20)
            .Select(i => client.WriteCoreRamAsync((uint)(i * 0x40), new byte[] { 0xAB }))
            .ToArray();

        await Task.WhenAll(readTasks.Cast<Task>().Concat(writeTasks));

        for (int i = 0; i < readOffsets.Length; i++)
        {
            byte expected = FakeRetroArchServer.ExpectedByte(readOffsets[i]);
            Assert.Equal(expected, readTasks[i].Result[0]);
        }
    }

    /// <summary>
    /// Regression test for a second, deeper bug found via live testing that survived the first
    /// fix (request serialization): a request whose reply arrives *after* the caller's own
    /// timeout already gave up on it doesn't vanish - it sits in the socket's receive buffer until
    /// some later, unrelated request's receive call picks it up, misattributing its data. This
    /// specifically exercises that "late reply from an abandoned request" pattern rather than
    /// plain concurrent load.
    /// </summary>
    [Fact]
    public async Task LateReplyFromATimedOutRequest_DoesNotCorruptALaterUnrelatedRequest()
    {
        using var server = new FakeRetroArchServer(delayFirstReplyBy: TimeSpan.FromMilliseconds(400));
        using var client = new RetroArchClient("127.0.0.1", server.Port, timeout: TimeSpan.FromMilliseconds(80));

        // First request: the server delays its reply well beyond the client's 80ms timeout, so
        // the first attempt inside ReadCoreRamAsync times out and retries; the retry (the server's
        // 2nd received request) is no longer "the first" so it gets a fast, correct reply.
        byte[] first = await client.ReadCoreRamAsync(0x100, 2);
        Assert.Equal(FakeRetroArchServer.ExpectedByte(0x100), first[0]);

        // A handful of further fast, unrelated reads while the *original* delayed reply for 0x100
        // (scheduled at the very start, still pending) is still somewhere between the server and
        // this client's socket buffer.
        for (uint offset = 0x200; offset <= 0x400; offset += 0x100)
        {
            byte[] result = await client.ReadCoreRamAsync(offset, 2);
            Assert.Equal(FakeRetroArchServer.ExpectedByte(offset), result[0]);
        }

        // Make sure the server's delayed original reply has definitely been sent by now, so it's
        // sitting in the socket's receive buffer (or arrives momentarily) when the next read below
        // calls ReceiveAsync.
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // This read must not be corrupted by the stale 0x100 reply waiting in the buffer.
        byte[] final = await client.ReadCoreRamAsync(0x500, 2);
        Assert.Equal(FakeRetroArchServer.ExpectedByte(0x500), final[0]);
    }

    /// <summary>
    /// A more precise, deterministic version of the above: the server sends 4 decoy (mismatched
    /// offset) replies immediately before the real one, for a single request. ReadCoreRamAsync
    /// only retries 3 times total, so a naive "accept whatever arrives first" implementation
    /// exhausts every retry on decoys and never reaches the real reply — this only passes if
    /// SendCommandAsync's discard loop skips mismatched replies *within* one attempt rather than
    /// burning attempts on them. (Confirmed failing without that loop before landing this test.)
    /// </summary>
    [Fact]
    public async Task DiscardsMultipleDecoyRepliesWithinASingleAttempt_WithoutExhaustingRetries()
    {
        using var server = new DecoyThenRealReplyFakeServer(decoyCount: 4);
        using var client = new RetroArchClient("127.0.0.1", server.Port, timeout: TimeSpan.FromMilliseconds(500));

        byte[] result = await client.ReadCoreRamAsync(0x700, 2);

        Assert.Equal(DecoyThenRealReplyFakeServer.ExpectedByte(0x700), result[0]);
    }
}

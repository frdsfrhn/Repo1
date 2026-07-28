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
}

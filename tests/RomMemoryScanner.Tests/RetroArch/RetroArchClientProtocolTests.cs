using RomMemoryScanner.Core.RetroArch;
using Xunit;

namespace RomMemoryScanner.Tests.RetroArch;

/// <summary>
/// Tests the READ_CORE_RAM reply parsing (FR-6.1) without touching the network — a real RetroArch
/// instance isn't available in CI, but the wire format is fixed and documented, so the parser is
/// tested against representative reply strings directly.
/// </summary>
public class RetroArchClientProtocolTests
{
    [Fact]
    public void ParseReadCoreRamReply_ParsesHexBytesInOrder()
    {
        byte[] bytes = RetroArchClient.ParseReadCoreRamReply("READ_CORE_RAM 7e0 8b 04", expectedOffset: 0x7e0, expectedLength: 2);

        Assert.Equal(new byte[] { 0x8b, 0x04 }, bytes);
    }

    [Fact]
    public void ParseReadCoreRamReply_IsCaseInsensitiveForCommandName()
    {
        byte[] bytes = RetroArchClient.ParseReadCoreRamReply("read_core_ram 0 ff", expectedOffset: 0, expectedLength: 1);
        Assert.Equal(new byte[] { 0xff }, bytes);
    }

    [Fact]
    public void ParseReadCoreRamReply_ThrowsOnFailureSentinel()
    {
        Assert.Throws<RetroArchCommunicationException>(
            () => RetroArchClient.ParseReadCoreRamReply("READ_CORE_RAM 7e0 -1", expectedOffset: 0x7e0, expectedLength: 2));
    }

    [Fact]
    public void ParseReadCoreRamReply_ThrowsWhenByteCountMismatched()
    {
        Assert.Throws<RetroArchCommunicationException>(
            () => RetroArchClient.ParseReadCoreRamReply("READ_CORE_RAM 7e0 8b", expectedOffset: 0x7e0, expectedLength: 2));
    }

    [Fact]
    public void ParseReadCoreRamReply_ThrowsOnUnrecognizedCommand()
    {
        Assert.Throws<RetroArchCommunicationException>(
            () => RetroArchClient.ParseReadCoreRamReply("SOMETHING_ELSE 7e0 8b", expectedOffset: 0x7e0, expectedLength: 1));
    }
}

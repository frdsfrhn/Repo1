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

    [Fact]
    public void ParseReadCoreRamReply_ThrowsOnStaleReplyForADifferentOffset()
    {
        // Regression test: a reply that arrived for an earlier chunk (rapid back-to-back UDP
        // requests during a scan pass) must not be silently accepted as the current one's data.
        var ex = Assert.Throws<RetroArchCommunicationException>(
            () => RetroArchClient.ParseReadCoreRamReply("READ_CORE_RAM 0 8b 04", expectedOffset: 0x2000, expectedLength: 2));
        Assert.Contains("mismatch", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("READ_CORE_RAM 7e0 8b zz")] // non-hex token
    [InlineData("READ_CORE_RAM 7e0 8 04")] // 1-char token, not a well-formed byte
    [InlineData("READ_CORE_RAM 7e0 8b0 04")] // 3-char token
    public void ParseReadCoreRamReply_ThrowsOnMalformedByteToken_InsteadOfLeakingFormatException(string reply)
    {
        Assert.Throws<RetroArchCommunicationException>(
            () => RetroArchClient.ParseReadCoreRamReply(reply, expectedOffset: 0x7e0, expectedLength: 2));
    }
}

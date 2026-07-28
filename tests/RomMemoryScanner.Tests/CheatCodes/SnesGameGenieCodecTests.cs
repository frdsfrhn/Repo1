using RomMemoryScanner.Core.CheatCodes;
using Xunit;

namespace RomMemoryScanner.Tests.CheatCodes;

/// <summary>
/// Fixtures generated from the independent, MIT-licensed "game-genie" npm reference
/// implementation (github.com/gjbianco/game-genie), whose alphabet matches §8a exactly. Used as
/// ground truth per the requirements doc's own validation strategy: "build a small fixture set of
/// known ... code ↔ address/value pairs ... and unit test encode/decode round-trips."
/// </summary>
public class SnesGameGenieCodecTests
{
    [Theory]
    [InlineData(0x7E0DB0u, (byte)0x09, "DBD8-9A78")]
    [InlineData(0x7E1FFFu, (byte)0xFF, "EEFE-2EE8")]
    [InlineData(0x7F0000u, (byte)0x00, "DDDD-FA7A")]
    [InlineData(0x7E0000u, (byte)0x01, "DFDD-FA76")]
    [InlineData(0x7FFFFFu, (byte)0xFE, "E3EE-2EEE")]
    [InlineData(0x008032u, (byte)0x7F, "5E67-DD6D")]
    public void Encode_MatchesReferenceImplementation(uint address, byte value, string expectedCode)
    {
        string code = SnesGameGenieCodec.Encode(address, value);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("DBD8-9A78", 0x7E0DB0u, (byte)0x09)]
    [InlineData("EEFE-2EE8", 0x7E1FFFu, (byte)0xFF)]
    [InlineData("DDDD-FA7A", 0x7F0000u, (byte)0x00)]
    [InlineData("DFDD-FA76", 0x7E0000u, (byte)0x01)]
    [InlineData("E3EE-2EEE", 0x7FFFFFu, (byte)0xFE)]
    [InlineData("5E67-DD6D", 0x008032u, (byte)0x7F)]
    public void Decode_MatchesReferenceImplementation(string code, uint expectedAddress, byte expectedValue)
    {
        CheatDecodeResult result = SnesGameGenieCodec.Decode(code);
        Assert.Equal(expectedAddress, result.ConsoleAddress);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(1, result.ValueByteWidth);
    }

    [Theory]
    [InlineData(0x7E0000u, (byte)0x00)]
    [InlineData(0x7FFFFFu, (byte)0xFF)]
    [InlineData(0x123456u, (byte)0xAB)]
    public void RoundTrip_EncodeThenDecode_RecoversOriginalValues(uint address, byte value)
    {
        string code = SnesGameGenieCodec.Encode(address, value);
        CheatDecodeResult result = SnesGameGenieCodec.Decode(code);

        Assert.Equal(address, result.ConsoleAddress);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Decode_AcceptsCodeWithoutDashOrLowercase()
    {
        CheatDecodeResult withDash = SnesGameGenieCodec.Decode("DBD8-9A78");
        CheatDecodeResult noDash = SnesGameGenieCodec.Decode("dbd89a78");

        Assert.Equal(withDash.ConsoleAddress, noDash.ConsoleAddress);
        Assert.Equal(withDash.Value, noDash.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOSHORT")]
    [InlineData("ZZZZZZZZ")] // 'Z' is not in the 16-symbol alphabet
    public void Decode_RejectsInvalidCodes(string invalidCode)
    {
        Assert.Throws<FormatException>(() => SnesGameGenieCodec.Decode(invalidCode));
        Assert.False(SnesGameGenieCodec.TryDecode(invalidCode, out _));
    }

    [Fact]
    public void Encode_RejectsAddressWiderThan24Bits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SnesGameGenieCodec.Encode(0x1000000, 0));
    }
}

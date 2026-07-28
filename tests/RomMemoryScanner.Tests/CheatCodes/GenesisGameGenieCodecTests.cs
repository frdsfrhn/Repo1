using RomMemoryScanner.Core.CheatCodes;
using Xunit;

namespace RomMemoryScanner.Tests.CheatCodes;

/// <summary>Fixtures generated from the same reference implementation as <see cref="SnesGameGenieCodecTests"/>.</summary>
public class GenesisGameGenieCodecTests
{
    [Theory]
    [InlineData(0xFF0000u, (ushort)0x0001, "AEAS-8AAA")]
    [InlineData(0xFFFE84u, (ushort)0x0063, "NS9S-8AEE")]
    [InlineData(0xFFFFFFu, (ushort)0xFFFF, "9999-9999")]
    [InlineData(0xFF1024u, (ushort)0x00FF, "96JS-8ABE")]
    [InlineData(0x000150u, (ushort)0x4E71, "REAT-A6WT")]
    public void Encode_MatchesReferenceImplementation(uint address, ushort value, string expectedCode)
    {
        string code = GenesisGameGenieCodec.Encode(address, value);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("AEAS-8AAA", 0xFF0000u, (ushort)0x0001)]
    [InlineData("NS9S-8AEE", 0xFFFE84u, (ushort)0x0063)]
    [InlineData("9999-9999", 0xFFFFFFu, (ushort)0xFFFF)]
    [InlineData("96JS-8ABE", 0xFF1024u, (ushort)0x00FF)]
    [InlineData("REAT-A6WT", 0x000150u, (ushort)0x4E71)]
    public void Decode_MatchesReferenceImplementation(string code, uint expectedAddress, ushort expectedValue)
    {
        CheatDecodeResult result = GenesisGameGenieCodec.Decode(code);
        Assert.Equal(expectedAddress, result.ConsoleAddress);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(2, result.ValueByteWidth);
    }

    [Theory]
    [InlineData(0xFF0000u, (ushort)0x0000)]
    [InlineData(0xFFFFFFu, (ushort)0xFFFF)]
    [InlineData(0x123456u, (ushort)0xABCD)]
    public void RoundTrip_EncodeThenDecode_RecoversOriginalValues(uint address, ushort value)
    {
        string code = GenesisGameGenieCodec.Encode(address, value);
        CheatDecodeResult result = GenesisGameGenieCodec.Decode(code);

        Assert.Equal(address, result.ConsoleAddress);
        Assert.Equal(value, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOSHORT")]
    [InlineData("IIIIIIII")] // 'I' is deliberately excluded from the Genesis alphabet
    public void Decode_RejectsInvalidCodes(string invalidCode)
    {
        Assert.Throws<FormatException>(() => GenesisGameGenieCodec.Decode(invalidCode));
        Assert.False(GenesisGameGenieCodec.TryDecode(invalidCode, out _));
    }

    [Fact]
    public void Encode_RejectsAddressWiderThan24Bits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GenesisGameGenieCodec.Encode(0x1000000, 0));
    }
}

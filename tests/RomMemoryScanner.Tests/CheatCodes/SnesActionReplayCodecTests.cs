using RomMemoryScanner.Core.CheatCodes;
using Xunit;

namespace RomMemoryScanner.Tests.CheatCodes;

/// <summary>
/// Fixture "7E747D84" (address 7E747D, value 84) matches the published SNES PAR example format
/// cited in community PAR documentation (6-digit address + 2-digit value, concatenated).
/// </summary>
public class SnesActionReplayCodecTests
{
    [Fact]
    public void Encode_ProducesColonSeparatedAddressAndValue()
    {
        string code = SnesActionReplayCodec.Encode(0x7E747D, 0x84);
        Assert.Equal("7E747D:84", code);
    }

    [Fact]
    public void Decode_AcceptsConcatenatedFormWithoutColon()
    {
        CheatDecodeResult result = SnesActionReplayCodec.Decode("7E747D84");
        Assert.Equal(0x7E747Du, result.ConsoleAddress);
        Assert.Equal(0x84u, result.Value);
        Assert.Equal(1, result.ValueByteWidth);
    }

    [Theory]
    [InlineData(0x7E0000u, (byte)0x00)]
    [InlineData(0x7FFFFFu, (byte)0xFF)]
    [InlineData(0x7E0DB0u, (byte)0x09)]
    public void RoundTrip_EncodeThenDecode_RecoversOriginalValues(uint address, byte value)
    {
        string code = SnesActionReplayCodec.Encode(address, value);
        CheatDecodeResult result = SnesActionReplayCodec.Decode(code);

        Assert.Equal(address, result.ConsoleAddress);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Encode_RejectsAddressOutsideWram()
    {
        Assert.Throws<Core.Addressing.AddressOutOfRangeException>(
            () => SnesActionReplayCodec.Encode(0x008032, 0x7F));
    }

    [Fact]
    public void Decode_RejectsAddressOutsideWram_AsFormatException()
    {
        Assert.Throws<FormatException>(() => SnesActionReplayCodec.Decode("008032:7F"));
        Assert.False(SnesActionReplayCodec.TryDecode("008032:7F", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOSHORT")]
    [InlineData("ZZZZZZ:ZZ")]
    public void Decode_RejectsInvalidCodes(string invalidCode)
    {
        Assert.Throws<FormatException>(() => SnesActionReplayCodec.Decode(invalidCode));
        Assert.False(SnesActionReplayCodec.TryDecode(invalidCode, out _));
    }
}

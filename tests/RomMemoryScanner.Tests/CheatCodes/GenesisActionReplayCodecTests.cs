using RomMemoryScanner.Core.CheatCodes;
using Xunit;

namespace RomMemoryScanner.Tests.CheatCodes;

/// <summary>
/// FF16EB:0014 is a real, empirically-confirmed code: entered directly into Kega Fusion's
/// "Game Genie / PAR" dialog against Shining in the Darkness (Genesis), it changed the item in
/// inventory slot 1 to a Mithril Sword (item id 0x14) — while the equivalent Game Genie encoding
/// of the same address did nothing, since Game Genie can't affect RAM (see
/// <see cref="GenesisGameGenieCodecTests"/> / <see cref="SnesGameGenieCodec"/> remarks).
/// </summary>
public class GenesisActionReplayCodecTests
{
    [Fact]
    public void Encode_MatchesRealWorldConfirmedCode()
    {
        string code = GenesisActionReplayCodec.Encode(0xFF16EB, 0x0014);
        Assert.Equal("FF16EB:0014", code);
    }

    [Fact]
    public void Decode_MatchesRealWorldConfirmedCode()
    {
        CheatDecodeResult result = GenesisActionReplayCodec.Decode("FF16EB:0014");
        Assert.Equal(0xFF16EBu, result.ConsoleAddress);
        Assert.Equal(0x0014u, result.Value);
        Assert.Equal(2, result.ValueByteWidth);
    }

    [Theory]
    [InlineData(0xFF0000u, (ushort)0x0000)]
    [InlineData(0xFFFFFFu, (ushort)0xFFFFu)]
    [InlineData(0xFF16EBu, (ushort)0x0014)]
    public void RoundTrip_EncodeThenDecode_RecoversOriginalValues(uint address, ushort value)
    {
        string code = GenesisActionReplayCodec.Encode(address, value);
        CheatDecodeResult result = GenesisActionReplayCodec.Decode(code);

        Assert.Equal(address, result.ConsoleAddress);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Decode_AcceptsCodeWithoutColon()
    {
        CheatDecodeResult withColon = GenesisActionReplayCodec.Decode("FF16EB:0014");
        CheatDecodeResult noColon = GenesisActionReplayCodec.Decode("FF16EB0014");

        Assert.Equal(withColon.ConsoleAddress, noColon.ConsoleAddress);
        Assert.Equal(withColon.Value, noColon.Value);
    }

    [Fact]
    public void Encode_RejectsAddressOutsideWram()
    {
        Assert.Throws<Core.Addressing.AddressOutOfRangeException>(
            () => GenesisActionReplayCodec.Encode(0x000150, 1));
    }

    [Fact]
    public void Decode_RejectsAddressOutsideWram_AsFormatException_NotAddressOutOfRangeException()
    {
        // Regression test: TryDecode only catches FormatException, so Decode must translate
        // AddressTranslator's AddressOutOfRangeException rather than let it leak through.
        Assert.Throws<FormatException>(() => GenesisActionReplayCodec.Decode("000150:0014"));
        Assert.False(GenesisActionReplayCodec.TryDecode("000150:0014", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("TOOSHORT")]
    [InlineData("ZZZZZZ:ZZZZ")]
    public void Decode_RejectsInvalidCodes(string invalidCode)
    {
        Assert.Throws<FormatException>(() => GenesisActionReplayCodec.Decode(invalidCode));
        Assert.False(GenesisActionReplayCodec.TryDecode(invalidCode, out _));
    }
}

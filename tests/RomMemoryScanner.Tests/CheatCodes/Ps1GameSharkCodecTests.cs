using RomMemoryScanner.Core.CheatCodes;
using RomMemoryScanner.Core.Models;
using Xunit;

namespace RomMemoryScanner.Tests.CheatCodes;

/// <summary>
/// PS1 GameShark/PAR has no bit-scrambling (§8a), so fixtures here are derived directly from the
/// documented format (code-type prefix byte + 24-bit RAM offset + value), corroborated across
/// multiple community references (GameHacking.org, Ethereal Games, ConsoleMods Wiki).
/// </summary>
public class Ps1GameSharkCodecTests
{
    [Theory]
    [InlineData(0x80010000u, 0x0064u, DataType.U16, "80010000 0064")]
    [InlineData(0x801FFFFFu, 0xFFFFu, DataType.U16, "801FFFFF FFFF")]
    [InlineData(0x80000000u, 0x0000u, DataType.U16, "80000000 0000")]
    public void Encode_16Bit_ProducesConstantWriteCode(uint address, uint value, DataType dataType, string expectedCode)
    {
        string code = Ps1GameSharkCodec.Encode(address, value, dataType);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData(0x80000000u, 0x0005u, DataType.U8, "30000000 0005")]
    [InlineData(0x800000FFu, 0x00FFu, DataType.U8, "300000FF 00FF")]
    public void Encode_8Bit_ProducesConstantWriteCode(uint address, uint value, DataType dataType, string expectedCode)
    {
        string code = Ps1GameSharkCodec.Encode(address, value, dataType);
        Assert.Equal(expectedCode, code);
    }

    [Theory]
    [InlineData("80010000 0064", 0x80010000u, 0x0064u, 2)]
    [InlineData("801FFFFF FFFF", 0x801FFFFFu, 0xFFFFu, 2)]
    [InlineData("30000000 0005", 0x80000000u, 0x0005u, 1)]
    public void Decode_ConstantWrite_RecoversAddressAndValue(string code, uint expectedAddress, uint expectedValue, int expectedWidth)
    {
        CheatDecodeResult result = Ps1GameSharkCodec.Decode(code);
        Assert.Equal(expectedAddress, result.ConsoleAddress);
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedWidth, result.ValueByteWidth);
    }

    [Theory]
    [InlineData("D0012345 0007", "equal")]
    [InlineData("D1012345 0007", "not equal")]
    [InlineData("E2012345 0007", "less than")]
    [InlineData("E3012345 0007", "greater than")]
    [InlineData("10012345 0001", "increment")]
    [InlineData("21012345 0001", "decrement")]
    public void Decode_RecognizesNonWriteCodeTypes(string code, string expectedDescriptionFragment)
    {
        CheatDecodeResult result = Ps1GameSharkCodec.Decode(code);
        Assert.Contains(expectedDescriptionFragment, result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RoundTrip_EncodeThenDecode_RecoversOriginalValues()
    {
        string code = Ps1GameSharkCodec.Encode(0x800ABCDE, 0x1234, DataType.U16);
        CheatDecodeResult result = Ps1GameSharkCodec.Decode(code);

        Assert.Equal(0x800ABCDEu, result.ConsoleAddress);
        Assert.Equal(0x1234u, result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NOTVALID")]
    [InlineData("FF000000 0000")] // 0xFF is not a defined code type
    public void Decode_RejectsInvalidCodes(string invalidCode)
    {
        Assert.Throws<FormatException>(() => Ps1GameSharkCodec.Decode(invalidCode));
        Assert.False(Ps1GameSharkCodec.TryDecode(invalidCode, out _));
    }

    [Fact]
    public void Encode_RejectsAddressOutsideRamWindow()
    {
        Assert.Throws<Core.Addressing.AddressOutOfRangeException>(
            () => Ps1GameSharkCodec.Encode(0x00010000, 1, DataType.U16));
    }

    [Fact]
    public void Encode_Rejects32BitDataType()
    {
        Assert.Throws<NotSupportedException>(
            () => Ps1GameSharkCodec.Encode(0x80010000, 1, DataType.U32));
    }
}

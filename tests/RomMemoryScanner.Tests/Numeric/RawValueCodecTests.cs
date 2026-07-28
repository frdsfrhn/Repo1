using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.Numeric;
using Xunit;

namespace RomMemoryScanner.Tests.Numeric;

public class RawValueCodecTests
{
    [Fact]
    public void FromBytes_LittleEndian_ReadsLeastSignificantByteFirst()
    {
        uint value = RawValueCodec.FromBytes(new byte[] { 0x34, 0x12 }, ByteOrder.LittleEndian);
        Assert.Equal(0x1234u, value);
    }

    [Fact]
    public void FromBytes_BigEndian_ReadsMostSignificantByteFirst()
    {
        uint value = RawValueCodec.FromBytes(new byte[] { 0x12, 0x34 }, ByteOrder.BigEndian);
        Assert.Equal(0x1234u, value);
    }

    [Fact]
    public void ToBytes_LittleEndian_WritesLeastSignificantByteFirst()
    {
        byte[] bytes = RawValueCodec.ToBytes(0x1234, 2, ByteOrder.LittleEndian);
        Assert.Equal(new byte[] { 0x34, 0x12 }, bytes);
    }

    [Fact]
    public void ToBytes_BigEndian_WritesMostSignificantByteFirst()
    {
        byte[] bytes = RawValueCodec.ToBytes(0x1234, 2, ByteOrder.BigEndian);
        Assert.Equal(new byte[] { 0x12, 0x34 }, bytes);
    }

    [Theory]
    [InlineData(ByteOrder.LittleEndian)]
    [InlineData(ByteOrder.BigEndian)]
    public void RoundTrip_ToBytesThenFromBytes_RecoversOriginalValue(ByteOrder order)
    {
        uint original = 0xABCDEFu;
        byte[] bytes = RawValueCodec.ToBytes(original, 4, order);
        uint recovered = RawValueCodec.FromBytes(bytes, order);
        Assert.Equal(original, recovered);
    }

    [Fact]
    public void ToBytes_SingleByte_IgnoresByteOrder()
    {
        Assert.Equal(new byte[] { 0x42 }, RawValueCodec.ToBytes(0x42, 1, ByteOrder.LittleEndian));
        Assert.Equal(new byte[] { 0x42 }, RawValueCodec.ToBytes(0x42, 1, ByteOrder.BigEndian));
    }
}

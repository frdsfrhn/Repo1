using RomMemoryScanner.Core.Numeric;
using Xunit;

namespace RomMemoryScanner.Tests.Numeric;

public class BcdConverterTests
{
    [Theory]
    [InlineData(0x00u, 1, 0u)]
    [InlineData(0x99u, 1, 99u)]
    [InlineData(0x1234u, 2, 1234u)]
    [InlineData(0x9999u, 2, 9999u)]
    public void BcdToDecimal_ConvertsCorrectly(uint bcd, int byteWidth, uint expectedDecimal)
    {
        Assert.Equal(expectedDecimal, BcdConverter.BcdToDecimal(bcd, byteWidth));
    }

    [Theory]
    [InlineData(0u, 1, 0x00u)]
    [InlineData(99u, 1, 0x99u)]
    [InlineData(1234u, 2, 0x1234u)]
    [InlineData(9999u, 2, 0x9999u)]
    public void DecimalToBcd_ConvertsCorrectly(uint decimalValue, int byteWidth, uint expectedBcd)
    {
        Assert.Equal(expectedBcd, BcdConverter.DecimalToBcd(decimalValue, byteWidth));
    }

    [Theory]
    [InlineData(100u, 1)] // 3-digit decimal doesn't fit in 1 BCD byte (max 99)
    [InlineData(10000u, 2)]
    public void DecimalToBcd_ThrowsWhenValueDoesNotFit(uint decimalValue, int byteWidth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BcdConverter.DecimalToBcd(decimalValue, byteWidth));
    }

    [Theory]
    [InlineData(0x1Au)] // 'A' nibble is not a valid decimal digit
    [InlineData(0xF0u)]
    public void BcdToDecimal_ThrowsForInvalidBcd(uint invalidBcd)
    {
        Assert.Throws<ArgumentException>(() => BcdConverter.BcdToDecimal(invalidBcd, 1));
    }

    [Theory]
    [InlineData(0u, 100000u)]
    [InlineData(1u, 99999u)]
    [InlineData(500u, 99000u)]
    public void RoundTrip_DecimalToBcdToDecimal_RecoversOriginalValue(uint low, uint high)
    {
        foreach (uint value in new[] { low, high })
        {
            uint bcd = BcdConverter.DecimalToBcd(value, 4);
            uint back = BcdConverter.BcdToDecimal(bcd, 4);
            Assert.Equal(value, back);
        }
    }

    [Fact]
    public void IsValidBcd_DetectsInvalidNibbles()
    {
        Assert.True(BcdConverter.IsValidBcd(0x99, 1));
        Assert.False(BcdConverter.IsValidBcd(0x9A, 1));
    }
}

using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;
using Xunit;

namespace RomMemoryScanner.Tests.Addressing;

public class AddressTranslatorTests
{
    [Theory]
    [InlineData(ConsoleType.Snes, 0x7E0000u, 0u)]
    [InlineData(ConsoleType.Snes, 0x7E0DB0u, 0x0DB0u)]
    [InlineData(ConsoleType.Snes, 0x7FFFFFu, 0x01FFFFu)]
    [InlineData(ConsoleType.Genesis, 0xFF0000u, 0u)]
    [InlineData(ConsoleType.Genesis, 0xFFFFFFu, 0xFFFFu)]
    [InlineData(ConsoleType.Ps1, 0x80000000u, 0u)]
    [InlineData(ConsoleType.Ps1, 0x801FFFFFu, 0x1FFFFFu)]
    public void ToCoreOffset_ConvertsConsoleAddressToZeroBasedOffset(ConsoleType console, uint consoleAddress, uint expectedOffset)
    {
        uint offset = AddressTranslator.ToCoreOffset(console, consoleAddress);
        Assert.Equal(expectedOffset, offset);
    }

    [Theory]
    [InlineData(ConsoleType.Snes, 0x000000u)] // ROM space, not WRAM
    [InlineData(ConsoleType.Snes, 0x800000u)] // just past WRAM
    [InlineData(ConsoleType.Genesis, 0x000000u)]
    [InlineData(ConsoleType.Ps1, 0x00000000u)] // physical mirror, not the KSEG0 address our model uses
    [InlineData(ConsoleType.Ps1, 0x80200000u)] // just past 2MB
    public void ToCoreOffset_ThrowsForAddressOutsideWram(ConsoleType console, uint consoleAddress)
    {
        Assert.Throws<AddressOutOfRangeException>(() => AddressTranslator.ToCoreOffset(console, consoleAddress));
    }

    [Fact]
    public void TryToCoreOffset_ReturnsFalseInsteadOfThrowing()
    {
        bool ok = AddressTranslator.TryToCoreOffset(ConsoleType.Snes, 0x000000, out uint offset);
        Assert.False(ok);
        Assert.Equal(0u, offset);
    }

    [Theory]
    [InlineData(ConsoleType.Snes, 0x0DB0u, 0x7E0DB0u)]
    [InlineData(ConsoleType.Genesis, 0xFFFFu, 0xFFFFFFu)]
    [InlineData(ConsoleType.Ps1, 0x1FFFFFu, 0x801FFFFFu)]
    public void ToConsoleAddress_IsInverseOfToCoreOffset(ConsoleType console, uint offset, uint expectedAddress)
    {
        uint address = AddressTranslator.ToConsoleAddress(console, offset);
        Assert.Equal(expectedAddress, address);
    }
}

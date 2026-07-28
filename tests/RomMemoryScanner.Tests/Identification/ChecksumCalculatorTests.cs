using System.Text;
using RomMemoryScanner.Core.Identification;
using RomMemoryScanner.Core.Models;
using Xunit;

namespace RomMemoryScanner.Tests.Identification;

public class ChecksumCalculatorTests
{
    // "123456789" is the standard CRC-32/ISO-HDLC catalogue check vector (crc32 == 0xCBF43926),
    // cross-checked independently against Python's zlib.crc32 during development.
    private static readonly byte[] StandardCheckVector = Encoding.ASCII.GetBytes("123456789");

    [Fact]
    public void Compute_Crc32_MatchesStandardCheckVector()
    {
        RomChecksum checksum = ChecksumCalculator.Compute(StandardCheckVector);
        Assert.Equal("cbf43926", checksum.Crc32);
    }

    [Fact]
    public void Compute_Md5_MatchesKnownHash()
    {
        RomChecksum checksum = ChecksumCalculator.Compute(StandardCheckVector);
        Assert.Equal("25f9e794323b453885f5181f1b624d0b", checksum.Md5);
    }

    [Fact]
    public void Compute_DifferentContent_ProducesDifferentChecksums()
    {
        RomChecksum a = ChecksumCalculator.Compute(Encoding.ASCII.GetBytes("alpha"));
        RomChecksum b = ChecksumCalculator.Compute(Encoding.ASCII.GetBytes("beta"));

        Assert.NotEqual(a.Crc32, b.Crc32);
        Assert.NotEqual(a.Md5, b.Md5);
    }

    [Fact]
    public void Compute_LeavesStreamPositionAtOriginalStart()
    {
        using var stream = new MemoryStream(StandardCheckVector);
        stream.Position = 0;
        ChecksumCalculator.Compute(stream);
        Assert.Equal(0, stream.Position);
    }
}

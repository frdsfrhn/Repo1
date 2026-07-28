using System.IO.Hashing;
using System.Security.Cryptography;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.Identification;

/// <summary>Computes CRC32/MD5 for a ROM file so it can be matched against the address database (FR-1.2).</summary>
public static class ChecksumCalculator
{
    public static RomChecksum Compute(Stream romStream)
    {
        if (!romStream.CanSeek)
        {
            throw new ArgumentException("ROM stream must be seekable to compute both checksums.", nameof(romStream));
        }

        long startPosition = romStream.Position;

        var crc32 = new Crc32();
        crc32.Append(romStream);
        string crcHex = Convert.ToHexString(crc32.GetCurrentHash()).ToLowerInvariant();
        // System.IO.Hashing.Crc32 returns bytes in little-endian order; reverse to the
        // conventional big-endian display order (e.g. matching No-Intro/Redump CRC32 listings).
        crcHex = string.Concat(crcHex.Chunk(2).Reverse().Select(c => new string(c)));

        romStream.Position = startPosition;
        using var md5 = MD5.Create();
        byte[] md5Hash = md5.ComputeHash(romStream);
        string md5Hex = Convert.ToHexString(md5Hash).ToLowerInvariant();

        romStream.Position = startPosition;

        return new RomChecksum { Crc32 = crcHex, Md5 = md5Hex };
    }

    public static RomChecksum Compute(byte[] romBytes)
    {
        using var stream = new MemoryStream(romBytes, writable: false);
        return Compute(stream);
    }
}

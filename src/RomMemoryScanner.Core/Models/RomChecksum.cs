using System.Text.Json.Serialization;

namespace RomMemoryScanner.Core.Models;

/// <summary>CRC32/MD5 pair used to match a loaded ROM to a database entry (FR-1.2, FR-3.4).</summary>
public sealed class RomChecksum
{
    [JsonPropertyName("crc32")]
    public string? Crc32 { get; set; }

    [JsonPropertyName("md5")]
    public string? Md5 { get; set; }

    public bool Matches(RomChecksum other)
    {
        if (Crc32 is not null && other.Crc32 is not null)
        {
            return string.Equals(Crc32, other.Crc32, StringComparison.OrdinalIgnoreCase);
        }

        if (Md5 is not null && other.Md5 is not null)
        {
            return string.Equals(Md5, other.Md5, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}

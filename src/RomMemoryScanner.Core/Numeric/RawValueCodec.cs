using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.Numeric;

/// <summary>Converts between a raw memory byte span and its integer value, honoring per-value byte order (FR-2.5/FR-4.1).</summary>
public static class RawValueCodec
{
    public static uint FromBytes(ReadOnlySpan<byte> bytes, ByteOrder byteOrder)
    {
        uint value = 0;
        if (byteOrder == ByteOrder.LittleEndian)
        {
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                value = (value << 8) | bytes[i];
            }
        }
        else
        {
            foreach (byte b in bytes)
            {
                value = (value << 8) | b;
            }
        }

        return value;
    }

    public static byte[] ToBytes(uint value, int byteWidth, ByteOrder byteOrder)
    {
        var bytes = new byte[byteWidth];
        for (int i = 0; i < byteWidth; i++)
        {
            byte b = (byte)((value >> (i * 8)) & 0xFF);
            int index = byteOrder == ByteOrder.LittleEndian ? i : byteWidth - 1 - i;
            bytes[index] = b;
        }

        return bytes;
    }
}

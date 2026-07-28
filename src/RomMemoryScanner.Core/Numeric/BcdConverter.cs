namespace RomMemoryScanner.Core.Numeric;

/// <summary>
/// Converts between raw bytes and binary-coded decimal, where each nibble holds one decimal digit
/// (0-9) — common for on-screen HP/Gold counters in retro games (FR-4.2).
/// </summary>
public static class BcdConverter
{
    /// <summary>Converts a BCD-encoded value (up to 4 bytes) to its decimal integer value.</summary>
    public static uint BcdToDecimal(uint bcdValue, int byteWidth)
    {
        uint result = 0;
        uint multiplier = 1;
        for (int nibbleIndex = 0; nibbleIndex < byteWidth * 2; nibbleIndex++)
        {
            uint digit = (bcdValue >> (nibbleIndex * 4)) & 0xF;
            if (digit > 9)
            {
                throw new ArgumentException($"Value 0x{bcdValue:X} is not valid BCD: nibble {nibbleIndex} is {digit} (>9).", nameof(bcdValue));
            }

            result += digit * multiplier;
            multiplier *= 10;
        }

        return result;
    }

    /// <summary>Converts a decimal integer to its BCD-encoded representation, packed into <paramref name="byteWidth"/> bytes.</summary>
    public static uint DecimalToBcd(uint decimalValue, int byteWidth)
    {
        uint maxDecimal = (uint)Math.Pow(10, byteWidth * 2) - 1;
        if (decimalValue > maxDecimal)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalValue), decimalValue, $"Value does not fit in {byteWidth} BCD byte(s) (max {maxDecimal}).");
        }

        uint result = 0;
        for (int nibbleIndex = 0; nibbleIndex < byteWidth * 2; nibbleIndex++)
        {
            uint digit = decimalValue % 10;
            result |= digit << (nibbleIndex * 4);
            decimalValue /= 10;
        }

        return result;
    }

    public static bool IsValidBcd(uint value, int byteWidth)
    {
        for (int nibbleIndex = 0; nibbleIndex < byteWidth * 2; nibbleIndex++)
        {
            if (((value >> (nibbleIndex * 4)) & 0xF) > 9)
            {
                return false;
            }
        }

        return true;
    }
}

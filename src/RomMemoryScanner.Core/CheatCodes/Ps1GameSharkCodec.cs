using System.Globalization;
using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.CheatCodes;

/// <summary>
/// Encodes/decodes PS1 GameShark / Pro Action Replay codes: two 4-byte hex groups,
/// "AAAAAAAA YYYY" (§8a). No bit-scrambling — the address field is a 1-byte code-type prefix
/// followed by a 3-byte RAM offset, and the value field is the (possibly zero-padded) value to
/// write. This is the simplest of the three formats, and per the requirements doc's own
/// recommendation (§8a "Validation strategy" / §9 Phase 2), the one to trust first.
/// </summary>
public static class Ps1GameSharkCodec
{
    /// <summary>Generates a constant-write code (FR-5.1) for the given console-space address and value.</summary>
    public static string Encode(uint consoleAddress, uint value, DataType dataType)
    {
        int byteWidth = dataType.ByteWidth();
        if (byteWidth > 2)
        {
            throw new NotSupportedException(
                "PS1 GameShark/PAR constant-write codes only support 8-bit and 16-bit values; use two codes for 32-bit values.");
        }

        uint offset = AddressTranslator.ToCoreOffset(ConsoleType.Ps1, consoleAddress);
        var codeType = byteWidth == 2 ? Ps1CodeType.ConstantWrite16 : Ps1CodeType.ConstantWrite8;

        uint maxValue = byteWidth == 2 ? 0xFFFFu : 0xFFu;
        if (value > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Value does not fit in {byteWidth * 8} bits.");
        }

        uint addressField = ((uint)codeType << 24) | (offset & 0x00FFFFFF);
        return $"{addressField:X8} {value:X4}";
    }

    public static bool TryDecode(string code, out CheatDecodeResult? result)
    {
        try
        {
            result = Decode(code);
            return true;
        }
        catch (FormatException)
        {
            result = null;
            return false;
        }
    }

    /// <summary>Decodes an existing code back to address+value, for FR-5.3 round-tripping.</summary>
    public static CheatDecodeResult Decode(string code)
    {
        string cleaned = code.Trim().Replace("-", " ").Replace(":", " ");
        string[] parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || parts[0].Length != 8 || parts[1].Length != 4)
        {
            throw new FormatException($"'{code}' is not a valid PS1 GameShark/PAR code (expected AAAAAAAA YYYY).");
        }

        if (!uint.TryParse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint addressField)
            || !uint.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint value))
        {
            throw new FormatException($"'{code}' contains non-hexadecimal digits.");
        }

        byte prefixByte = (byte)(addressField >> 24);
        if (!Enum.IsDefined(typeof(Ps1CodeType), prefixByte))
        {
            throw new FormatException($"'{code}' uses an unrecognized code-type prefix 0x{prefixByte:X2}.");
        }

        var codeType = (Ps1CodeType)prefixByte;
        uint offset = addressField & 0x00FFFFFF;
        uint consoleAddress = AddressTranslator.ToConsoleAddress(ConsoleType.Ps1, offset);

        if (codeType.ValueByteWidth() == 1)
        {
            value &= 0xFF;
        }

        return new CheatDecodeResult(consoleAddress, value, codeType.ValueByteWidth(), codeType.Describe());
    }
}

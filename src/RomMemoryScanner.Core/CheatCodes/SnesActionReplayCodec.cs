using System.Globalization;
using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.CheatCodes;

/// <summary>
/// Encodes/decodes SNES Pro Action Replay (PAR) codes: an 8-hex-digit string, the first 6 digits
/// a WRAM address (conventionally 7E0000-7FFFFF) and the last 2 an 8-bit value to poke there — no
/// bit-scrambling, unlike Game Genie (§8a covers Game Genie only; PAR is the format §3's table
/// also lists for SNES, and is what actually affects live RAM — see <see cref="SnesGameGenieCodec"/>
/// remarks on why Game Genie can't).
///
/// Confirmed against real usage: this is the same shape empirically verified to work for Genesis
/// via Kega Fusion (see <see cref="GenesisActionReplayCodec"/>), and matches the documented SNES
/// PAR convention (e.g. published codes like "7E747D84" = address 7E747D, value 84).
/// </summary>
public static class SnesActionReplayCodec
{
    public static string Encode(uint consoleAddress, byte value)
    {
        AddressTranslator.ToCoreOffset(ConsoleType.Snes, consoleAddress); // validates WRAM bounds
        return $"{consoleAddress:X6}:{value:X2}";
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

    public static CheatDecodeResult Decode(string code)
    {
        string cleaned = code.Trim().Replace(":", "").Replace("-", "").Replace(" ", "");
        if (cleaned.Length != 8)
        {
            throw new FormatException($"'{code}' is not a valid SNES PAR code (expected 6-digit address + 2-digit value, e.g. 7E0DB0:09).");
        }

        if (!uint.TryParse(cleaned.Substring(0, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint address)
            || !byte.TryParse(cleaned.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
        {
            throw new FormatException($"'{code}' contains non-hexadecimal digits.");
        }

        try
        {
            AddressTranslator.ToCoreOffset(ConsoleType.Snes, address); // validates WRAM bounds
        }
        catch (AddressOutOfRangeException ex)
        {
            throw new FormatException($"'{code}' decodes to an address outside SNES WRAM: {ex.Message}", ex);
        }

        return new CheatDecodeResult(address, value, 1, "SNES Pro Action Replay");
    }
}

using System.Globalization;
using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.CheatCodes;

/// <summary>
/// Encodes/decodes Genesis/Mega Drive Pro Action Replay (PAR) codes: a 6-hex-digit WRAM address
/// (conventionally FF0000-FFFFFF) plus a 4-hex-digit (16-bit) value — no bit-scrambling, unlike
/// Game Genie. Genesis PAR writes are word-only per its hardware (address bit 0 isn't decoded),
/// hence the 16-bit value width, vs. SNES PAR's 8-bit.
///
/// This exact shape — "FF16EB:0014" — was confirmed working directly in Kega Fusion's own
/// "Game Genie / PAR" code entry during development (poking a Genesis RPG's inventory slot),
/// distinct from that same dialog's Game Genie field, which does not affect RAM.
/// </summary>
public static class GenesisActionReplayCodec
{
    public static string Encode(uint consoleAddress, ushort value)
    {
        AddressTranslator.ToCoreOffset(ConsoleType.Genesis, consoleAddress); // validates WRAM bounds
        return $"{consoleAddress:X6}:{value:X4}";
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
        if (cleaned.Length != 10)
        {
            throw new FormatException($"'{code}' is not a valid Genesis PAR code (expected 6-digit address + 4-digit value, e.g. FF16EB:0014).");
        }

        if (!uint.TryParse(cleaned.Substring(0, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint address)
            || !ushort.TryParse(cleaned.Substring(6, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort value))
        {
            throw new FormatException($"'{code}' contains non-hexadecimal digits.");
        }

        try
        {
            AddressTranslator.ToCoreOffset(ConsoleType.Genesis, address); // validates WRAM bounds
        }
        catch (AddressOutOfRangeException ex)
        {
            throw new FormatException($"'{code}' decodes to an address outside Genesis WRAM: {ex.Message}", ex);
        }

        return new CheatDecodeResult(address, value, 2, "Genesis Pro Action Replay");
    }
}

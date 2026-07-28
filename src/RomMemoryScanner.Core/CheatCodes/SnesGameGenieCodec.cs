namespace RomMemoryScanner.Core.CheatCodes;

/// <summary>
/// Encodes/decodes SNES Game Genie codes (§8a): an 8-character code drawn from the 16-symbol
/// alphabet "DF4709156BC8A23E", decoding to a 24-bit console address (6 hex digits) and an 8-bit
/// value (2 hex digits) via a fixed bit permutation — not simple concatenation.
///
/// Unlike GameShark/PAR (which conventionally only targets the live RAM window), original Game
/// Genie hardware intercepted the cartridge/ROM address bus, so decoded addresses are NOT
/// restricted to WRAM here — a pasted code may legitimately reference ROM space (FR-5.3). WRAM
/// bounds are only enforced where a value is actually applied live, in
/// <see cref="Addressing.AddressTranslator"/>.
///
/// The permutation implemented here matches the widely-used open-source "game-genie" reference
/// implementation (github.com/gjbianco/game-genie, MIT), whose alphabet and address/value split
/// match this requirements document exactly, and was independently round-trip verified against
/// generated fixtures during development (see <c>SnesGameGenieCodecTests</c>).
/// </summary>
public static class SnesGameGenieCodec
{
    private const string Alphabet = "DF4709156BC8A23E";

    public static string Encode(uint consoleAddress, byte value)
    {
        if (consoleAddress > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(consoleAddress), consoleAddress, "SNES Game Genie addresses are 24-bit (max 0xFFFFFF).");
        }

        string raw = consoleAddress.ToString("X6") + value.ToString("X2");
        var nibs = new string[8];
        for (int i = 0; i < 8; i++)
        {
            nibs[i] = Convert.ToString(Convert.ToInt32(raw[i].ToString(), 16), 2).PadLeft(4, '0');
        }

        string temp1 = nibs[3].Substring(2, 2) + nibs[0].Substring(0, 2) + nibs[0].Substring(2, 2);
        string bits = nibs[6] + nibs[7] + nibs[2] + nibs[4] + temp1 + nibs[5] + nibs[1] + nibs[3].Substring(0, 2);

        var code = new System.Text.StringBuilder();
        for (int i = 0; i < 8; i++)
        {
            string nibble = bits.Substring(i * 4, 4);
            int index = Convert.ToInt32(nibble, 2);
            code.Append(Alphabet[index]);
            if (i == 3)
            {
                code.Append('-');
            }
        }

        return code.ToString();
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
        string cleaned = code.Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();
        if (cleaned.Length != 8 || cleaned.Any(c => Alphabet.IndexOf(c) < 0))
        {
            throw new FormatException($"'{code}' is not a valid SNES Game Genie code (8 letters from {Alphabet}).");
        }

        string bits = string.Concat(cleaned.Select(c => Convert.ToString(Alphabet.IndexOf(c), 2).PadLeft(4, '0')));

        var nibs = new string[8];
        nibs[0] = bits.Substring(18, 4);
        nibs[1] = bits.Substring(26, 4);
        nibs[2] = bits.Substring(8, 4);
        nibs[3] = bits.Substring(30, 2) + bits.Substring(16, 2);
        nibs[4] = bits.Substring(12, 4);
        nibs[5] = bits.Substring(22, 4);
        nibs[6] = bits.Substring(0, 4);
        nibs[7] = bits.Substring(4, 4);

        var hexDigits = new char[8];
        for (int i = 0; i < 8; i++)
        {
            hexDigits[i] = Convert.ToInt32(nibs[i], 2).ToString("X")[0];
        }

        string hex = new string(hexDigits);
        uint consoleAddress = Convert.ToUInt32(hex.Substring(0, 6), 16);
        byte value = Convert.ToByte(hex.Substring(6, 2), 16);

        return new CheatDecodeResult(consoleAddress, value, 1, "SNES Game Genie");
    }
}

namespace RomMemoryScanner.Core.CheatCodes;

/// <summary>
/// Encodes/decodes Genesis/Mega Drive Game Genie codes (§8a): a 9-character code (8 symbols plus
/// a cosmetic dash) drawn from the 32-symbol alphabet "ABCDEFGHJKLMNPRSTVWXYZ0123456789" (I, O, Q,
/// U excluded to avoid confusion with other characters), decoding to a 24-bit address and 16-bit
/// value via a fixed bit permutation distinct from the SNES one.
///
/// As with SNES, decoded addresses are not restricted to WRAM here (see
/// <see cref="SnesGameGenieCodec"/> remarks) — original Genesis Game Genie hardware intercepted
/// the 68000 address bus generally, not just work RAM.
///
/// The permutation implemented here matches the widely-used open-source "game-genie" reference
/// implementation (github.com/gjbianco/game-genie, MIT), whose alphabet matches this requirements
/// document exactly, and was independently round-trip verified against generated fixtures during
/// development (see <c>GenesisGameGenieCodecTests</c>).
/// </summary>
public static class GenesisGameGenieCodec
{
    private const string Alphabet = "ABCDEFGHJKLMNPRSTVWXYZ0123456789";

    public static string Encode(uint consoleAddress, ushort value)
    {
        if (consoleAddress > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(consoleAddress), consoleAddress, "Genesis Game Genie addresses are 24-bit (max 0xFFFFFF).");
        }

        string raw = consoleAddress.ToString("X6") + value.ToString("X4");
        var nibs = new string[10];
        for (int i = 0; i < 10; i++)
        {
            nibs[i] = Convert.ToString(Convert.ToInt32(raw[i].ToString(), 16), 2).PadLeft(4, '0');
        }

        string temp1 = nibs[6].Substring(3, 1) + nibs[7].Substring(0, 3);
        string temp2 = nibs[7].Substring(3, 1) + nibs[6].Substring(0, 3);
        string bits = nibs[8] + nibs[9] + nibs[2] + nibs[3] + nibs[0] + nibs[1] + temp1 + temp2 + nibs[4] + nibs[5];

        var code = new System.Text.StringBuilder();
        for (int i = 0; i < 8; i++)
        {
            string symbolBits = bits.Substring(i * 5, 5);
            int index = Convert.ToInt32(symbolBits, 2);
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
            throw new FormatException($"'{code}' is not a valid Genesis Game Genie code (8 symbols from {Alphabet}).");
        }

        string bits = string.Concat(cleaned.Select(c => Convert.ToString(Alphabet.IndexOf(c), 2).PadLeft(5, '0')));

        var nibs = new string[10];
        nibs[0] = bits.Substring(16, 4);
        nibs[1] = bits.Substring(20, 4);
        nibs[2] = bits.Substring(8, 4);
        nibs[3] = bits.Substring(12, 4);
        nibs[4] = bits.Substring(32, 4);
        nibs[5] = bits.Substring(36, 4);
        nibs[6] = bits.Substring(29, 3) + bits.Substring(24, 1);
        nibs[7] = bits.Substring(25, 4);
        nibs[8] = bits.Substring(0, 4);
        nibs[9] = bits.Substring(4, 4);

        var hexDigits = new char[10];
        for (int i = 0; i < 10; i++)
        {
            hexDigits[i] = Convert.ToInt32(nibs[i], 2).ToString("X")[0];
        }

        string hex = new string(hexDigits);
        uint consoleAddress = Convert.ToUInt32(hex.Substring(0, 6), 16);
        ushort value = Convert.ToUInt16(hex.Substring(6, 4), 16);

        return new CheatDecodeResult(consoleAddress, value, 2, "Genesis Game Genie");
    }
}
